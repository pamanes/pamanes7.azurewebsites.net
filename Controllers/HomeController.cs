using Blog.Models;
using Blog.Services;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blog.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MarkdownPipeline _markdownPipeline;

        public HomeController(ILogger<HomeController> logger, MarkdownPipeline markdownPipeline)
        {
            _logger = logger;
            _markdownPipeline = markdownPipeline;
        }

        public async Task<IActionResult> Index([FromServices] MDBlogDbContext db)
        {
            var latestPosts = await db.Posts
                .OrderByDescending(p => p.Date)
                .Take(10)
                .ToListAsync();

            return View(latestPosts);
        }

        public async Task<IActionResult> Search([FromServices] MDBlogDbContext db, string q = null, int page = 1)
        {
            const int pageSize = 10;

            // Filter posts by search if provided
            var query = db.Posts.AsQueryable();
            var search = q?.ToLower();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => EF.Functions.Like(p.Title, $"%{search}%") || EF.Functions.Like(p.Tags, $"%{search}%") || EF.Functions.Like(p.Markdown, $"%{search}%"));
            }

            var totalPosts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            var posts = await query
                .OrderByDescending(p => p.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = q;

            return View(posts);
        }
        [Authorize]
        public IActionResult Create()
        {
            var post = new MDBlogPost
            {
                Markdown = string.Empty
            };

            return View(post);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(MDBlogPost post, [FromServices] MDBlogDbContext db)
        {
            
            FrontMatter metadata = null;
            string content = null;
            //validate markdown
            try
            {
                (metadata, content) = FrontMatterParser.Parse(post.Markdown ?? string.Empty);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"YAML front matter is invalid: {ex.Message} {ex.InnerException?.Message}");
            }
            catch (FormatException ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"Date format in front matter is invalid: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"YAML front matter error: {ex.Message}");
            }

            if (metadata != null && content != null && ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(post.Title?.Trim()))
                {
                    ModelState.AddModelError(nameof(post.Markdown), "Front matter must include a 'title' field.");
                }
                
                // Validate tags: only letters, numbers, and hyphens/underscores allowed
                var invalidTags = post.Tags?.Split(",")
                    .Select(t => t.Trim())
                    .Where(t => !Regex.IsMatch(t, @"^[a-zA-Z0-9_-]+$"))
                    .ToList();

                if (invalidTags?.Any() ?? false)
                {
                    var bad = string.Join(", ", invalidTags);
                    ModelState.AddModelError(nameof(post.Markdown), $"Invalid tag(s): {bad}. Only letters, digits, hyphens, and underscores are allowed.");
                }
            }
            
            if (!ModelState.IsValid)
            {
                if (post.Id > 0)
                {
                    return View("Edit", post);
                }
                else
                {
                    return View("Create", post);
                }
            }

            // Build front matter
            var now = DateTime.Now;
            var tagsList = post.Tags?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            var fm = new FrontMatter
            {
                Title = post.Title.Trim(),
                Subtitle = post.Subtitle?.Trim(),
                Tags = tagsList,
                Date = now,
                LastUpdated = now
            };
            string yaml = FrontMatterSerializer.Serialize(fm);

            // Combine
            string fullMarkdown = $"{yaml}\n{post.Markdown?.Trim()}\n";

            var blogPost = post.Id > 0
                ? await db.Posts.FindAsync(post.Id)
                : new MDBlogPost { Date = now };

            if (blogPost == null)
            {
                return NotFound();
            }

            blogPost.Title = post.Title.Trim();            
            blogPost.Subtitle = post.Subtitle?.Trim();
            blogPost.Markdown = fullMarkdown;
            blogPost.Tags = string.Join(",", post.Tags);
            blogPost.Date = post.Id == 0 ? now : blogPost.Date;
            blogPost.LastUpdated = now;
            var rawSlug = GenerateSlug(blogPost.Title);
            blogPost.Slug = $"{blogPost.Date:yyyy-MM-dd}-{rawSlug}";
            blogPost.Path = $"{blogPost.Date:yyyy/MM/dd}/{rawSlug}.html";

            if (post.Id == 0)
            {
                db.Posts.Add(blogPost);
            }
            bool errorOnSave = false;
            try
            {
                await db.SaveChangesAsync();
            }
            catch(Exception ex) when (ex.InnerException is SqliteException && ((SqliteException)ex.InnerException).SqliteErrorCode == 19) // Unique constraint violation
            {
                ModelState.AddModelError(nameof(post.Title), "A post with this title already exists. Please choose a different title.");
                errorOnSave = true;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"Save error: {ex.Message}");
                errorOnSave = true;
            }

            if (errorOnSave)
            {
                if (post.Id > 0)
                {
                    return View("Edit", post);
                }
                else
                {
                    return View("Create", post);
                }
            }

            return RedirectToRoute(
                routeName: "PostByDateSlugMarkdig",
                routeValues: new
                {
                    year = blogPost.Date.Year,
                    month = blogPost.Date.Month.ToString("D2"),
                    day = blogPost.Date.Day.ToString("D2"),
                    slug = rawSlug
                }
            );
        }
        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            // Normalize to decompose accents (e.g., "título" → "título")
            string normalized = title.Normalize(NormalizationForm.FormD);

            // Remove diacritics (accents)
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Convert to lowercase and replace non-alphanumeric with hyphens
            string cleaned = Regex.Replace(sb.ToString().ToLowerInvariant(), @"[^a-z0-9]+", "-");

            // Trim leading/trailing hyphens
            return cleaned.Trim('-');
        }
        public async Task<IActionResult> Post(int id, [FromServices] MDBlogDbContext db)
        {
            var post = await db.Posts.FindAsync(id);
            if (post == null) return NotFound();
            return View("Post", post);
        }
        [Authorize]
        public async Task<IActionResult> Edit(int id, [FromServices] MDBlogDbContext db)
        {
            var editPost = await db.Posts.FindAsync(id);
            if (editPost == null) return NotFound();

            //strip frontMatter
            var (metadata, body) = FrontMatterParser.Parse(editPost.Markdown);

            editPost.Markdown = body;
            return View(editPost);
        }
        public async Task<IActionResult> PostsByTag([FromServices] MDBlogDbContext db)
        {
            var posts = await db.Posts
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            // Extract unique tags
            var allTags = posts
                .SelectMany(p => (p.Tags ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Group posts by each tag
            var postsByTag = allTags.ToDictionary(
                tag => tag,
                tag => posts.Where(p =>
                    (p.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Contains(tag))
                    .ToList()
            );

            var model = new PostsByTagViewModel
            {
                Tags = allTags,
                PostsByTag = postsByTag
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> PostByDateSlugMarkdig(int year, int month, int day, string slug, [FromServices] MDBlogDbContext db)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var datePrefix = new DateTime(year, month, day).ToString("yyyy-MM-dd");
            var fullSlug = $"{datePrefix}-{slug}";

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == fullSlug);
            if (post == null)
                return NotFound();

            //strip frontMatter
            var (metadata, body) = FrontMatterParser.Parse(post.Markdown);

            //fix links
            MarkdownDocument document = Markdown.Parse(body, _markdownPipeline);
            foreach (LinkInline link in document.Descendants<LinkInline>())
            {                
                if (!(link.Url?.StartsWith("#") ?? false))
                    link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }

            foreach (AutolinkInline link in document.Descendants<AutolinkInline>())
            {

                if (!(link.Url?.StartsWith("#") ?? false))
                    link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }

            var viewModel = new MDBlogPostViewModel
            {
                Id = post.Id,
                Title = metadata.Title,
                Subtitle = metadata.Subtitle,
                Tags = metadata.Tags ?? Enumerable.Empty<string>(),
                Date = metadata.Date,
                LastUpdated = metadata.LastUpdated,
                HtmlContent = document.ToHtml(_markdownPipeline)
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, bool search, string q, [FromServices] MDBlogDbContext db)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Forbid();
            }

            var post = await db.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            db.Posts.Remove(post);
            await db.SaveChangesAsync();
            if (!search)
                return RedirectToAction("Index");
            else
                return RedirectToAction("Search", new { q });
        }
        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
