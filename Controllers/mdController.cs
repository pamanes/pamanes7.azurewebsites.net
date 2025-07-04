using Blog.Models;
using Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blog.Controllers
{
    public class mdBlogController : Controller
    {
        public async Task<IActionResult> Index([FromServices] MDBlogDbContext db, string q = "", int page = 1)
        {            
            const int pageSize = 10;

            // Filter posts by search if provided
            var query = db.Posts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Title.Contains(q) || p.Tags.Contains(q));
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
            var now = DateTimeOffset.Now;

            var frontMatter =
                "---\n" +
                $"title:\n" +
                "tags:\n" +
                "- tag1\n" +
                $"date: {now:yyyy-MM-dd HH:mm zzz}\n" +
                $"last-updated: {now:yyyy-MM-dd HH:mm zzz}\n" +
                "---\n\n";

            var post = new MDBlogPost
            {
                Markdown = frontMatter
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
            try
            {
                (metadata, content) = FrontMatterParser.Parse(post.Markdown);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"YAML front matter is invalid: {ex.Message} {ex.InnerException?.Message}");
            }
            catch (FormatException ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"Date format in front matter is invalid: {ex.Message}");
            }

            if (metadata != null && content != null && ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(metadata.Title?.Trim()))
                {
                    ModelState.AddModelError(nameof(post.Markdown), "Front matter must include a 'title' field.");
                }

                // Validate tags: only letters, numbers, and hyphens/underscores allowed
                var invalidTags = metadata.Tags
                    .Select(t => t.Trim())
                    .Where(t => !Regex.IsMatch(t, @"^[a-zA-Z0-9_-]+$"))
                    .ToList();

                if (invalidTags.Any())
                {
                    var bad = string.Join(", ", invalidTags);
                    ModelState.AddModelError(nameof(post.Markdown), $"Invalid tag(s): {bad}. Only letters, digits, hyphens, and underscores are allowed.");
                }
            }

            if(!ModelState.IsValid)
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

            var blogPost = post.Id > 0
                ? await db.Posts.FindAsync(post.Id)
                : new MDBlogPost { Date = DateTime.UtcNow };

            if (blogPost == null)
            {
                return NotFound();
            }

            blogPost.Title = metadata.Title.Trim();
            blogPost.Slug = GenerateSlug(blogPost.Title);
            blogPost.Subtitle = metadata.Subtitle?.Trim() ?? string.Empty;
            blogPost.Markdown = content;
            blogPost.Tags = string.Join(",", metadata.Tags) ?? string.Empty;
            blogPost.Date = post.Id == 0 ? metadata.Date ?? DateTime.Now : blogPost.Date;
            blogPost.LastUpdated = metadata.LastUpdated ?? DateTime.Now;
            var rawSlug = GenerateSlug(metadata.Title);
            var datePart = blogPost.Date.ToString("yyyy-MM-dd");
            var fullSlug = $"{datePart}-{rawSlug}";
            blogPost.Slug = fullSlug;
            blogPost.Path = $"{blogPost.Date:yyyy/MM/dd}/{rawSlug}.html";

            if (post.Id == 0)
            {
                db.Posts.Add(blogPost);
            }

            await db.SaveChangesAsync();
            var newId = blogPost.Id;
            return RedirectToAction("Post", new { id = newId });
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
        public async Task<IActionResult> Edit(int id, [FromServices] MDBlogDbContext db)
        {
            var editPost = await db.Posts.FindAsync(id);
            if (editPost == null) return NotFound();

            string[] tagList = (editPost.Tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToArray();
            // Rebuild front matter string
            StringBuilder frontMatter = new StringBuilder();
            frontMatter.Append($"---\n");
            frontMatter.Append($"title: {editPost.Title}\n");
            frontMatter.Append($"subtitle: {editPost.Subtitle}\n");
            frontMatter.Append($"tags:\n{string.Join("\n", tagList.Select(t => $"- {t}"))}\n");
            frontMatter.Append($"date: {editPost.Date.ToString("yyyy-MM-dd HH:mm zzz")}\n");
            frontMatter.Append($"last-Updated: {editPost.LastUpdated.ToString("yyyy-MM-dd HH:mm zzz")}\n");              
            frontMatter.Append($"---\n\n");

            editPost.Markdown = frontMatter.ToString() + editPost.Markdown;
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
        public async Task<IActionResult> PostByDateSlug(int year, int month, int day, string slug, [FromServices] MDBlogDbContext db)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var datePrefix = new DateTime(year, month, day).ToString("yyyy-MM-dd");
            var fullSlug = $"{datePrefix}-{slug}";

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == fullSlug);
            if (post == null)
                return NotFound();

            return View("Post", post);
        }
    }
}
