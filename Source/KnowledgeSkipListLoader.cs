using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Verse;

namespace GenKnowledge
{
    public sealed class KnowledgeSkipRuleSet
    {
        private readonly HashSet<string> skipExactLogicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keepExactLogicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Regex> skipLogicalKeyPatterns = new List<Regex>();
        private readonly List<Regex> keepLogicalKeyPatterns = new List<Regex>();
        private readonly List<string> skipContentContains = new List<string>();
        private readonly List<string> skipTagContains = new List<string>();
        private readonly List<Regex> skipContentRegex = new List<Regex>();

        public int ApproxRuleCount =>
            skipExactLogicalKeys.Count +
            keepExactLogicalKeys.Count +
            skipLogicalKeyPatterns.Count +
            keepLogicalKeyPatterns.Count +
            skipContentContains.Count +
            skipTagContains.Count +
            skipContentRegex.Count;

        public void MergeFrom(KnowledgeSkipRuleSet other)
        {
            if (other == null)
            {
                return;
            }

            foreach (string key in other.skipExactLogicalKeys)
            {
                skipExactLogicalKeys.Add(key);
            }

            foreach (string key in other.keepExactLogicalKeys)
            {
                keepExactLogicalKeys.Add(key);
            }

            skipLogicalKeyPatterns.AddRange(other.skipLogicalKeyPatterns);
            keepLogicalKeyPatterns.AddRange(other.keepLogicalKeyPatterns);
            skipContentContains.AddRange(other.skipContentContains);
            skipTagContains.AddRange(other.skipTagContains);
            skipContentRegex.AddRange(other.skipContentRegex);
        }

        public bool ShouldSkip(GeneratedKnowledgeItem item)
        {
            if (item == null)
            {
                return false;
            }

            string logicalKey = item.LogicalKey ?? string.Empty;
            string tag = item.Tag ?? string.Empty;
            string content = item.Content ?? string.Empty;

            if (IsForcedKeep(logicalKey))
            {
                return false;
            }

            if (skipExactLogicalKeys.Contains(logicalKey))
            {
                return true;
            }

            for (int i = 0; i < skipLogicalKeyPatterns.Count; i++)
            {
                if (skipLogicalKeyPatterns[i].IsMatch(logicalKey))
                {
                    return true;
                }
            }

            for (int i = 0; i < skipTagContains.Count; i++)
            {
                if (tag.IndexOf(skipTagContains[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            for (int i = 0; i < skipContentContains.Count; i++)
            {
                if (content.IndexOf(skipContentContains[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            for (int i = 0; i < skipContentRegex.Count; i++)
            {
                if (skipContentRegex[i].IsMatch(content))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsForcedKeep(string logicalKey)
        {
            if (keepExactLogicalKeys.Contains(logicalKey))
            {
                return true;
            }

            for (int i = 0; i < keepLogicalKeyPatterns.Count; i++)
            {
                if (keepLogicalKeyPatterns[i].IsMatch(logicalKey))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddLogicalKeyRule(string logicalKey, bool keep)
        {
            if (string.IsNullOrWhiteSpace(logicalKey))
            {
                return;
            }

            logicalKey = logicalKey.Trim();
            bool wildcard = logicalKey.IndexOf('*') >= 0 || logicalKey.IndexOf('?') >= 0;

            if (wildcard)
            {
                Regex regex = WildcardToRegex(logicalKey);
                if (keep)
                {
                    keepLogicalKeyPatterns.Add(regex);
                }
                else
                {
                    skipLogicalKeyPatterns.Add(regex);
                }

                return;
            }

            if (keep)
            {
                keepExactLogicalKeys.Add(logicalKey);
            }
            else
            {
                skipExactLogicalKeys.Add(logicalKey);
            }
        }

        public void AddContentContainsRule(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                skipContentContains.Add(token.Trim());
            }
        }

        public void AddTagContainsRule(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                skipTagContains.Add(token.Trim());
            }
        }

        public bool TryAddContentRegexRule(string regexPattern, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(regexPattern))
            {
                return true;
            }

            try
            {
                skipContentRegex.Add(new Regex(regexPattern.Trim(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static Regex WildcardToRegex(string pattern)
        {
            string escaped = Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".");
            return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }

    public static class KnowledgeSkipListLoader
    {
        public const string RealWorldListRelativePath = "1.6/Data/KnowledgeSkipList.txt";
        public const string HighRedundancyListRelativePath = "1.6/Data/KnowledgeSkipList.HighRedundancy.txt";

        public static KnowledgeSkipRuleSet LoadRulesForRelativePath(string relativePath, out string loadError, out string loadedPath)
        {
            loadError = null;
            loadedPath = null;
            var ruleSet = new KnowledgeSkipRuleSet();

            string path = ResolveListPath(relativePath);
            if (string.IsNullOrWhiteSpace(path))
            {
                return ruleSet;
            }

            loadedPath = path;
            if (!File.Exists(path))
            {
                return ruleSet;
            }

            var errors = new List<string>();
            try
            {
                int lineNo = 0;
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    lineNo++;
                    if (!TryParseRuleLine(rawLine, ruleSet, out string parseError) && !string.IsNullOrWhiteSpace(parseError))
                    {
                        errors.Add($"line {lineNo}: {parseError}");
                    }
                }
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
                return new KnowledgeSkipRuleSet();
            }

            if (errors.Count > 0)
            {
                loadError = string.Join("; ", errors);
            }

            return ruleSet;
        }

        private static string ResolveListPath(string relativePath)
        {
            string normalized = (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            string modRoot = GenKnowledgeMod.ModRootDir;
            if (!string.IsNullOrWhiteSpace(modRoot))
            {
                return Path.Combine(modRoot, normalized);
            }

            try
            {
                return Path.Combine(GenFilePaths.ModsFolderPath, "RimTalk_GenKnowledge", normalized);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryParseRuleLine(string line, KnowledgeSkipRuleSet target, out string error)
        {
            error = null;
            if (target == null)
            {
                error = "target is null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            string text = line.Trim();
            if (text.Length == 0 ||
                text.StartsWith("#", StringComparison.Ordinal) ||
                text.StartsWith("//", StringComparison.Ordinal) ||
                text.StartsWith(";", StringComparison.Ordinal))
            {
                return true;
            }

            bool keep = false;
            if (text.StartsWith("!", StringComparison.Ordinal))
            {
                keep = true;
                text = text.Substring(1).Trim();
                if (text.Length == 0)
                {
                    return true;
                }
            }

            if (TryParsePrefixedRule(text, target, keep, out error))
            {
                return true;
            }

            string logicalKey = ExtractLogicalKey(text);
            if (string.IsNullOrWhiteSpace(logicalKey))
            {
                error = $"unrecognized rule: {text}";
                return false;
            }

            target.AddLogicalKeyRule(logicalKey, keep);
            return true;
        }

        private static bool TryParsePrefixedRule(string text, KnowledgeSkipRuleSet target, bool keep, out string error)
        {
            error = null;
            int sep = text.IndexOf(':');
            if (sep <= 0)
            {
                return false;
            }

            string prefix = text.Substring(0, sep).Trim();
            string payload = sep + 1 < text.Length ? text.Substring(sep + 1).Trim() : string.Empty;
            if (prefix.Length == 0 || payload.Length == 0)
            {
                return false;
            }

            if (prefix.Equals("logicalKey", StringComparison.OrdinalIgnoreCase) ||
                prefix.Equals("key", StringComparison.OrdinalIgnoreCase))
            {
                target.AddLogicalKeyRule(payload, keep);
                return true;
            }

            if (keep)
            {
                error = $"keep override only supports logicalKey rules: {text}";
                return false;
            }

            if (prefix.Equals("content", StringComparison.OrdinalIgnoreCase))
            {
                target.AddContentContainsRule(payload);
                return true;
            }

            if (prefix.Equals("tag", StringComparison.OrdinalIgnoreCase))
            {
                target.AddTagContainsRule(payload);
                return true;
            }

            if (prefix.Equals("regex", StringComparison.OrdinalIgnoreCase))
            {
                if (!target.TryAddContentRegexRule(payload, out string regexError))
                {
                    error = $"invalid regex '{payload}': {regexError}";
                    return false;
                }

                return true;
            }

            return false;
        }

        private static string ExtractLogicalKey(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string[] parts = line.Split('|');
            if (parts.Length >= 2)
            {
                string fromSecond = parts[1].Trim();
                if (fromSecond.IndexOf(':') > 0)
                {
                    return fromSecond;
                }
            }

            string fromFirst = parts[0].Trim();
            if (fromFirst.IndexOf(':') > 0)
            {
                return fromFirst;
            }

            return null;
        }
    }
}
