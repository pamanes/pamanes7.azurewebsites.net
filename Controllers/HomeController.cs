using Blog.Models;
using Blog.Services;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.ApplicationInsights;
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
        private readonly MarkdownPipeline _markdownPipeline;
        private IDeserializer _frontMatterDeserializer;
        private IPostService _postService;

        public HomeController(MarkdownPipeline markdownPipeline, IDeserializer frontMatterDeserializer, IPostService postService)
        {
            _markdownPipeline = markdownPipeline;
            _frontMatterDeserializer = frontMatterDeserializer;
            _postService = postService;
        }

        public async Task<IActionResult> Index()
        {
            var latestPosts = await _postService.GetLatestPosts();
            return View(latestPosts);
        }

        public async Task<IActionResult> Search(string q = null, int page = 1)
        {
            var searchResulsts = await _postService.Search(q, page);
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
                post.Tags = post.Tags?.Trim().TrimStart(',').TrimEnd(',');
                // Validate tags: only letters, numbers, and hyphens/underscores allowed
                var invalidTags = post.Tags?.Split(",")
                    .Select(t => t.Trim())
                    .Where(t => !Regex.IsMatch(t, @"^[a-zA-Z0-9_-]+$"))
                    .ToList();

                if (invalidTags?.Any() ?? false)
                {
                    var bad = string.Join(", ", invalidTags);
                    ModelState.AddModelError(nameof(post.Tags), $"Invalid tag(s): {bad}. Only letters, digits, hyphens, and underscores are allowed.");
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

            post.Title = post.Title.Trim();
            post.Subtitle = post.Subtitle?.Trim();
            post.Markdown = post.Markdown?.Trim();

            try
            {
                post = await _postService.SavePost(post);
                if (post == null)
                {
                    ModelState.AddModelError(nameof(post.Markdown), "Post not found or could not be updated.");
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
                    year = post.Date.Year,
                    month = post.Date.Month.ToString("D2"),
                    day = post.Date.Day.ToString("D2"),
                    slug = MarkdownHelperFunctions.GenerateSlug(post.Title)
                }
            );
        }
        
        public async Task<IActionResult> Post(int id)
        {
            var post = await _postService.GetPostById(id);
            if (post == null) return NotFound();
            return View("Post", post);
        }
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var editPost = await _postService.GetPostById(id);
            if (editPost == null) return NotFound();

            //strip frontMatter
            var (metadata, body) = _frontMatterDeserializer.Parse(editPost.Markdown);

            editPost.Markdown = body;
            return View(editPost);
        }
        public async Task<IActionResult> PostsByTag()
        {
            (IEnumerable<string> tags, Dictionary<string, List<MDBlogPost>> postsByTag) = await _postService.GetPostsByTag();

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

            var post = await _postService.GetPostByFullSlug(fullSlug);
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

            await _postService.DeletePostById(id);
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
