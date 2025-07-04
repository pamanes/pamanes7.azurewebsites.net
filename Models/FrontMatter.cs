using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace Blog.Models
{
    public class FrontMatter
    {
        public string Title { get; set; } = string.Empty;
        [YamlMember(Alias = "subtitle")]
        public string Subtitle { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public DateTime? Date { get; set; }
        [YamlMember(Alias = "last-updated")]
        public DateTime? LastUpdated { get; set; }
    }
    public static class FrontMatterSerializer
    {
        public static string Serialize(FrontMatter fm)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine($"title: {fm.Title}");
            sb.AppendLine($"subtitle: {fm.Subtitle}");
            sb.AppendLine("tags:");
            foreach (var t in fm.Tags ?? Enumerable.Empty<string>())
            {
                sb.AppendLine($"- {t}");
            }
            sb.AppendLine($"date: {fm.Date:yyyy-MM-dd HH:mm zzz}");
            sb.AppendLine($"last-updated: {fm.LastUpdated:yyyy-MM-dd HH:mm zzz}");
            sb.AppendLine("---");
            return sb.ToString();
        }
    }
}
