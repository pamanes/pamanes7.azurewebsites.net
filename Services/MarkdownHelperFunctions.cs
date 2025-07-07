using Blog.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
namespace Blog.Services
{
    public static class MarkdownHelperFunctions
    {
        public static (FrontMatter metadata, string body) Parse(this IDeserializer deserializer, string markdown)
        {
            var regex = new Regex(@"^---\s*\r?\n(?<yaml>.*?)\r?\n---\s*\r?\n(?<content>.*)", RegexOptions.Singleline);
            var match = regex.Match(markdown);

            if (!match.Success)
            {
                // No front matter, return empty metadata and full content
                return (new FrontMatter(), markdown);
            }

            var yaml = match.Groups["yaml"].Value;
            var content = match.Groups["content"].Value;

            var metadata = deserializer.Deserialize<FrontMatter>(yaml) ?? new FrontMatter();

            return (metadata, content.Trim());
        }
        public static string GenerateSlug(string title)
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
    }
}
