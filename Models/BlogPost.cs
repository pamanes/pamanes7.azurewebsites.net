using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Blog.Models
{
    public class MDBlogPost
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Path { get; set; }
        [MaxLength(250)]
        public string Subtitle { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string Markdown { get; set; } = string.Empty;
    }

    public class MDBlogPostSearchResults
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string Search { get; set; }
        public IEnumerable<MDBlogPost> Posts { get; set; } = new List<MDBlogPost>();
    }

public class MDBlogPostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; } = string.Empty;
        public IEnumerable<string> Tags { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string HtmlContent { get; set; }
    }
}
