using System.Collections.Generic;

namespace Blog.Models
{
    public class PostsByTagViewModel
    {
        public IEnumerable<string> Tags { get; set; }
        public Dictionary<string, List<MDBlogPost>> PostsByTag { get; set; }
    }
}
