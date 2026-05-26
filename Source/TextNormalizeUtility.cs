using System;
using System.Linq;

namespace GenKnowledge
{
    public static class TextNormalizeUtility
    {
        public static string NormalizeMultiline(string text, string lineSeparator)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] parts = normalized
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p?.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string sep = lineSeparator ?? "\n";
            return string.Join(sep, parts);
        }
    }
}
