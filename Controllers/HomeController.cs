using Blog.Models;
using Blog.Services;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Blog.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MarkdownPipeline _markdownPipeline;
        private IDeserializer _frontMatterDeserializer;
        private IDataService _dataService;

        public HomeController(ILogger<HomeController> logger, MarkdownPipeline markdownPipeline, IDeserializer frontMatterDeserializer, IDataService dataService)
        {
            _logger = logger;
            _markdownPipeline = markdownPipeline;
            _frontMatterDeserializer = frontMatterDeserializer;
            _dataService = dataService;
        }

        public async Task<IActionResult> Index()
        {
            var latestPosts = await _dataService.GetLatestPosts();
            return View(latestPosts);
        }

        public async Task<IActionResult> Search(string q = null, int page = 1)
        {
            var searchResulsts = await _dataService.Search(q, page);
            return View(searchResulsts);
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
        public async Task<IActionResult> Save(MDBlogPost post)
        {
            
            FrontMatter metadata = null;
            string content = null;
            //validate markdown
            try
            {
                (metadata, content) = _frontMatterDeserializer.Parse(post.Markdown ?? string.Empty);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"Error: {ex.Message}");
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

            var blogPost = new MDBlogPost();
            blogPost.Id = post.Id;
            blogPost.Title = post.Title.Trim();            
            blogPost.Subtitle = post.Subtitle?.Trim();
            blogPost.Markdown = fullMarkdown;
            blogPost.Tags = string.Join(",", post.Tags);
            blogPost.Date = post.Id == 0 ? now : blogPost.Date;
            blogPost.LastUpdated = now;
            var rawSlug = MarkdownHelperFunctions.GenerateSlug(blogPost.Title);
            blogPost.Slug = $"{blogPost.Date:yyyy-MM-dd}-{rawSlug}";
            blogPost.Path = $"{blogPost.Date:yyyy/MM/dd}/{rawSlug}.html";

            try
            {
                if (post.Id > 0)
                {
                    blogPost = await _dataService.EditPost(blogPost);
                    if (blogPost == null)
                    {
                        ModelState.AddModelError(nameof(post.Markdown), "Post not found or could not be updated.");
                    }
                }
                else
                {
                    blogPost = await _dataService.CreatePost(blogPost);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(nameof(post.Markdown), $"Save error: {ex.Message}");
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
        
        public async Task<IActionResult> Post(int id)
        {
            var post = _dataService.GetPostById(id);
            if (post == null) return NotFound();
            return View("Post", post);
        }
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var editPost = await _dataService.GetPostById(id);
            if (editPost == null) return NotFound();

            //strip frontMatter
            var (metadata, body) = _frontMatterDeserializer.Parse(editPost.Markdown);

            editPost.Markdown = body;
            return View(editPost);
        }
        public async Task<IActionResult> PostsByTag()
        {
            (IEnumerable<string> tags, Dictionary<string, List<MDBlogPost>> postsByTag) = await _dataService.GetPostsByTag();

            var model = new PostsByTagViewModel
            {
                Tags = tags,
                PostsByTag = postsByTag
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> PostByDateSlugMarkdig(int year, int month, int day, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var datePrefix = new DateTime(year, month, day).ToString("yyyy-MM-dd");
            var fullSlug = $"{datePrefix}-{slug}";

            var post = await _dataService.GetPostByFullSlug(fullSlug);
            if (post == null)
                return NotFound();

            //strip frontMatter
            var (metadata, body) = _frontMatterDeserializer.Parse(post.Markdown);

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
        public async Task<IActionResult> Delete(int id, bool search, string q)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Forbid();
            }

            await _dataService.DeletePostById(id);
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
