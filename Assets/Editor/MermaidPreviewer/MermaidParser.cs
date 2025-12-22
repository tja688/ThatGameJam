using System.Text.RegularExpressions;

namespace MermaidPreviewer
{
    internal static class MermaidParser
    {
        private static readonly Regex FencedRegex = new Regex(
            "```mermaid\\s*\\r?\\n([\\s\\S]*?)\\r?\\n```",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ColonRegex = new Regex(
            ":::mermaid\\s*\\r?\\n([\\s\\S]*?)\\r?\\n:::",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryExtractFirst(string text, out string mermaid)
        {
            mermaid = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var match = FencedRegex.Match(text);
            if (match.Success)
            {
                mermaid = match.Groups[1].Value.Trim();
                return !string.IsNullOrEmpty(mermaid);
            }

            match = ColonRegex.Match(text);
            if (match.Success)
            {
                mermaid = match.Groups[1].Value.Trim();
                return !string.IsNullOrEmpty(mermaid);
            }

            return false;
        }
    }
}
