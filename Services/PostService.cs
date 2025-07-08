using Blog.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Services
{
    public interface IPostService
    {
        Task<IEnumerable<MDBlogPost>> GetLatestPosts();
        Task<MDBlogPostSearchResults> Search(string search, int page = 1);
        Task<MDBlogPost> GetPostById(int id);
        Task DeletePostById(int id);
        Task<(IEnumerable<string>, Dictionary<string, List<MDBlogPost>>)> GetPostsByTag();
        Task<MDBlogPost> GetPostByFullSlug(string fullSlug);
        Task<MDBlogPost> SavePost(MDBlogPost post);
    }
    public class PostService : IPostService
    {
        private MDBlogDbContext _db;
        public PostService(MDBlogDbContext db)
        {
            _db = db;
        }
        public async Task<(IEnumerable<string>, Dictionary<string, List<MDBlogPost>>)> GetPostsByTag()
        {
            var posts = await _db.Posts
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
            return (allTags, postsByTag);
        }
        public async Task<IEnumerable<MDBlogPost>> GetLatestPosts()
        {
            var posts = await _db.Posts
                .OrderByDescending(p => p.Date)
                .Take(10)
                .ToListAsync();
            return posts;
        }
        public async Task DeletePostById(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post != null)
            {
                _db.Posts.Remove(post);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<MDBlogPost> SavePost(MDBlogPost post)
        {
            MDBlogPost postToSave;
            bool isNew = post.Id == 0;

            if (isNew)
            {
                postToSave = post;
            }
            else
            {
                postToSave = await _db.Posts.FindAsync(post.Id);
                if (postToSave == null)
                    return null;

                postToSave.LastUpdated = post.LastUpdated;
            }

            var fm = new FrontMatter
            {
                Title = post.Title.Trim(),
                Subtitle = post.Subtitle?.Trim(),
                Tags = post.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
                Date = postToSave.Date,
                LastUpdated = post.LastUpdated
            };

            string yaml = FrontMatterSerializer.Serialize(fm);
            var rawSlug = MarkdownHelperFunctions.GenerateSlug(post.Title);

            postToSave.Title = post.Title.Trim();
            postToSave.Subtitle = post.Subtitle?.Trim();
            postToSave.Markdown = $"{yaml}\n{post.Markdown?.Trim()}\n";
            postToSave.Tags = post.Tags;
            postToSave.Slug = $"{postToSave.Date:yyyy-MM-dd}-{rawSlug}";
            postToSave.Path = $"{postToSave.Date:yyyy/MM/dd}/{rawSlug}.html";

            if (isNew)
                _db.Posts.Add(postToSave);

            await _db.SaveChangesAsync();
            return postToSave;
        }

        public async Task<MDBlogPost> GetPostById(int id)
        {
            var post = await _db.Posts.FindAsync(id);
            return post;
        }
        public async Task<MDBlogPost> GetPostByFullSlug(string fullSlug)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Slug == fullSlug);
            return post;
        }
        public async Task<MDBlogPostSearchResults> Search(string search, int page = 1)
        {
            const int pageSize = 10;
            var query = _db.Posts.AsQueryable();
            search = search?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(search))
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
            var searchResults = new MDBlogPostSearchResults()
            {
                CurrentPage = page,
                TotalPages = totalPages,
                Search = search,
                Posts = posts
            };
            return searchResults;
        }
    }
}
