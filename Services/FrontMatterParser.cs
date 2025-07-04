using Blog.Models;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace Blog.Services
{
    public static class FrontMatterParser
    {
        public static (FrontMatter metadata, string body) Parse(string markdown)
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

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var metadata = deserializer.Deserialize<FrontMatter>(yaml) ?? new FrontMatter();

            return (metadata, content.Trim());
        }
    }

}
