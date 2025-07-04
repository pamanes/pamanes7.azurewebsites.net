using System;
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
}
