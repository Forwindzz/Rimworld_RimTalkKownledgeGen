using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefPlaceholderUtility
    {
        public static string ProcessTemplateString(
            string templateString,
            IEnumerable<PlaceholderDescriptor> placeholders,
            Func<string, string> resolveValueByKey)
        {
            if (string.IsNullOrWhiteSpace(templateString))
            {
                return null;
            }

            string result = templateString;
            if (placeholders != null)
            {
                foreach (PlaceholderDescriptor placeholder in placeholders)
                {
                    if (placeholder == null || string.IsNullOrWhiteSpace(placeholder.Token))
                    {
                        continue;
                    }

                    string value = resolveValueByKey?.Invoke(placeholder.Key) ?? string.Empty;
                    result = result.Replace(placeholder.Token, value);
                }
            }

            return result.Trim();
        }

        public static string BuildPlaceholderHint(IEnumerable<PlaceholderDescriptor> placeholders)
        {
            if (placeholders == null)
            {
                return "RimTalkGenKnowledge.Settings.PlaceholdersNone".Translate();
            }

            string[] tokens = placeholders
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Token))
                .Select(p => p.Token)
                .Distinct()
                .ToArray();

            if (tokens.Length == 0)
            {
                return "RimTalkGenKnowledge.Settings.PlaceholdersNone".Translate();
            }

            return "RimTalkGenKnowledge.Settings.PlaceholdersHint".Translate(string.Join(", ", tokens));
        }

        public static void ShowInsertPlaceholderMenu(
            IEnumerable<PlaceholderDescriptor> placeholders,
            Action<string> applyText,
            string currentText)
        {
            if (placeholders == null || applyText == null)
            {
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (PlaceholderDescriptor placeholder in placeholders)
            {
                if (placeholder == null || string.IsNullOrWhiteSpace(placeholder.Token))
                {
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(placeholder.Description)
                    ? placeholder.Token
                    : $"{placeholder.Token} - {placeholder.Description}";

                string token = placeholder.Token;
                options.Add(new FloatMenuOption(label, () =>
                {
                    applyText(AppendPlaceholder(currentText, token));
                }));
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static string AppendPlaceholder(string currentText, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return currentText ?? string.Empty;
            }

            if (string.IsNullOrEmpty(currentText))
            {
                return token;
            }

            return currentText + token;
        }

        public static string NormalizeTags(string rawTags)
        {
            if (string.IsNullOrWhiteSpace(rawTags))
            {
                return null;
            }

            string normalized = rawTags
                .Replace('，', ',')
                .Replace('、', ',');

            string[] tags = normalized
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToArray();

            if (tags.Length == 0)
            {
                return null;
            }

            return string.Join(",", tags);
        }
    }
}
