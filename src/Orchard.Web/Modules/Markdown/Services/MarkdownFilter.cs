using System;
using Orchard.Services;

namespace Markdown.Services {
    public class MarkdownFilter : IHtmlFilter {
        public string ProcessContent(string text, string flavor) {
            return String.Equals(flavor, "markdown", StringComparison.OrdinalIgnoreCase) ? MarkdownReplace(text) : text;
        }

        private static string MarkdownReplace(string text) {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Markdig.Markdown.ToHtml(text);
        }
    }
}
