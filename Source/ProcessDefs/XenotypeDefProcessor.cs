using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class XenotypeDefProcessor : IProcessDef
    {
        public const string ProcessorId = "XenotypeDefProcessor";

        private string currentLabel;
        private string currentDescription;

        public string Id => ProcessorId;
        public string DisplayName => "RimTalkGenKnowledge.Processor.Xenotype.DisplayName".Translate();

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

        public void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            typed.Enabled = true;
            typed.TagTemplate = "{{label}}";
            typed.KnowledgeTemplate = "{{label}}: {{description}}";
            typed.BaseImportance = 0.5f;
        }

        public IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return BuildPlaceholderDescriptors();
        }

        public string ProcessTemplateString(string templateString, ProcessDefBaseConfig config)
        {
            return ProcessDefPlaceholderUtility.ProcessTemplateString(
                templateString,
                BuildPlaceholderDescriptors(),
                ResolveValueByKey);
        }

        public float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 220f;
        }

        public void DrawConfig(Rect rect, ProcessDefBaseConfig config)
        {
            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig;
            if (typed == null)
            {
                Widgets.Label(rect, "RimTalkGenKnowledge.Message.InvalidXenotypeConfigType".Translate());
                return;
            }

            float y = rect.y;
            const float lineHeight = 24f;
            const float gap = 6f;

            Rect enabledRect = new Rect(rect.x, y, rect.width, lineHeight);
            Widgets.CheckboxLabeled(enabledRect, "RimTalkGenKnowledge.Settings.Enabled".Translate(), ref typed.Enabled);
            y += lineHeight + gap;

            y = DrawTemplateRow(new Rect(rect.x, y, rect.width, lineHeight), "RimTalkGenKnowledge.Settings.TagTemplate".Translate(), typed.TagTemplate, value => typed.TagTemplate = value);
            y += gap;
            y = DrawTemplateRow(new Rect(rect.x, y, rect.width, lineHeight), "RimTalkGenKnowledge.Settings.KnowledgeTemplate".Translate(), typed.KnowledgeTemplate, value => typed.KnowledgeTemplate = value);
            y += gap;

            Rect resetRect = new Rect(rect.x, y, 120f, lineHeight);
            if (Widgets.ButtonText(resetRect, "RimTalkGenKnowledge.Settings.Reset".Translate()))
            {
                ApplyDefaultConfig(typed);
            }

            y += lineHeight + gap;
            Rect importanceLabelRect = new Rect(rect.x, y, rect.width, lineHeight);
            Widgets.Label(importanceLabelRect, "RimTalkGenKnowledge.Settings.BaseImportance".Translate(typed.BaseImportance.ToString("0.00")));
            y += lineHeight;

            Rect sliderRect = new Rect(rect.x, y, rect.width, lineHeight);
            typed.BaseImportance = Widgets.HorizontalSlider(sliderRect, typed.BaseImportance, 0f, 1f, false);
            y += lineHeight + gap;

            string placeholderText = ProcessDefPlaceholderUtility.BuildPlaceholderHint(BuildPlaceholderDescriptors());
            Rect placeholderRect = new Rect(rect.x, y, rect.width, lineHeight);
            Widgets.Label(placeholderRect, placeholderText);
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

                currentLabel = label;
                currentDescription = description;

                string rawTag = ProcessTemplateString(tagTemplate, typed);
                string tag = ProcessDefPlaceholderUtility.NormalizeTags(rawTag);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                string content = ProcessTemplateString(knowledgeTemplate, typed);
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

            currentLabel = null;
            currentDescription = null;
        }

        private float DrawTemplateRow(Rect rowRect, string label, string value, Action<string> setter)
        {
            const float labelWidth = 140f;
            const float buttonWidth = 62f;
            const float gap = 6f;

            Rect labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            Widgets.Label(labelRect, label);

            float textWidth = rowRect.width - labelWidth - buttonWidth - gap;
            if (textWidth < 40f)
            {
                textWidth = 40f;
            }

            Rect textRect = new Rect(labelRect.xMax, rowRect.y, textWidth, rowRect.height);
            string nextValue = Widgets.TextField(textRect, value ?? string.Empty);
            if (!string.Equals(nextValue, value, StringComparison.Ordinal))
            {
                setter(nextValue);
            }

            Rect insertRect = new Rect(textRect.xMax + gap, rowRect.y, buttonWidth, rowRect.height);
            if (Widgets.ButtonText(insertRect, "RimTalkGenKnowledge.Settings.Insert".Translate()))
            {
                ProcessDefPlaceholderUtility.ShowInsertPlaceholderMenu(
                    BuildPlaceholderDescriptors(),
                    setter,
                    nextValue ?? string.Empty);
            }

            return rowRect.yMax;
        }

        private string ResolveValueByKey(string key)
        {
            switch (key)
            {
                case "label":
                    return currentLabel ?? string.Empty;
                case "description":
                    return currentDescription ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static List<PlaceholderDescriptor> BuildPlaceholderDescriptors()
        {
            return new List<PlaceholderDescriptor>
            {
                new PlaceholderDescriptor
                {
                    Key = "label",
                    Token = "{{label}}",
                    Description = "RimTalkGenKnowledge.Placeholder.Xenotype.Label".Translate(),
                    ExampleValue = "Baseliner"
                },
                new PlaceholderDescriptor
                {
                    Key = "description",
                    Token = "{{description}}",
                    Description = "RimTalkGenKnowledge.Placeholder.Xenotype.Description".Translate(),
                    ExampleValue = "A baseline human xenotype."
                }
            };
        }

    }
}
