using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class XenotypeDefProcessor : IProcessDef
    {
        public const string ProcessorId = "XenotypeDefProcessor";

        public string Id => ProcessorId;
        public string DisplayName => "XenotypeDef";

        public ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new XenotypeProcessDefConfig
            {
                Enabled = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}}",
                BaseImportance = 0.5f
            };
        }

        public float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 164f;
        }

        public void DrawConfig(Rect rect, ProcessDefBaseConfig config)
        {
            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig;
            if (typed == null)
            {
                Widgets.Label(rect, "Invalid Xenotype config type.");
                return;
            }

            var listing = new Listing_Standard();
            listing.Begin(rect);
            listing.CheckboxLabeled("Enabled", ref typed.Enabled);
            typed.TagTemplate = listing.TextEntryLabeled("Tag Template", typed.TagTemplate ?? string.Empty);
            typed.KnowledgeTemplate = listing.TextEntryLabeled("Knowledge Template", typed.KnowledgeTemplate ?? string.Empty);
            listing.Label($"Base Importance: {typed.BaseImportance:0.00}");
            typed.BaseImportance = listing.Slider(typed.BaseImportance, 0f, 1f);
            listing.End();
        }

        public IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            if (!ModsConfig.BiotechActive)
            {
                yield break;
            }

            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig ?? (XenotypeProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            string tagTemplate = string.IsNullOrWhiteSpace(typed.TagTemplate) ? "{{label}}" : typed.TagTemplate;
            string knowledgeTemplate = string.IsNullOrWhiteSpace(typed.KnowledgeTemplate) ? "{{label}}: {{description}}" : typed.KnowledgeTemplate;
            float importance = Mathf.Clamp01(typed.BaseImportance);

            List<XenotypeDef> defs = DefDatabase<XenotypeDef>.AllDefsListForReading;
            if (defs == null)
            {
                yield break;
            }

            foreach (XenotypeDef def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

                string label = def.label?.Trim();
                string description = def.description?.Trim();
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                string rawTag = ApplyTemplate(tagTemplate, label, description);
                string tag = NormalizeTags(rawTag);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                string content = ApplyTemplate(knowledgeTemplate, label, description);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = $"xenotype:{def.defName}",
                    Tag = tag,
                    Content = content,
                    Importance = importance
                };
            }
        }

        private static string ApplyTemplate(string template, string label, string description)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            return template
                .Replace("{{label}}", label ?? string.Empty)
                .Replace("{{description}}", description ?? string.Empty)
                .Trim();
        }

        private static string NormalizeTags(string rawTags)
        {
            if (string.IsNullOrWhiteSpace(rawTags))
            {
                return null;
            }

            // Memory mod supports multi-tag format with English comma separators.
            string normalized = rawTags
                .Replace('，', ',')
                .Replace('、', ',');

            var tags = normalized
                .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
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
