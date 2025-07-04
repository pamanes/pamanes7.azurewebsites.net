using System.Collections.Generic;

namespace Blog.Models
{
    public class PostsByTagViewModel
    {
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, List<MDBlogPost>> PostsByTag { get; set; } = new();
    }
}
