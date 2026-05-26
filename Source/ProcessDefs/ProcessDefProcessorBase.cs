using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public abstract class ProcessDefProcessorBase<TConfig> : IProcessDef where TConfig : ProcessDefBaseConfig
    {
        private Dictionary<string, string> currentValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract ProcessDefBaseConfig CreateDefaultConfig();
        public abstract void ApplyDefaultConfig(ProcessDefBaseConfig config);
        public abstract IEnumerable<PlaceholderDescriptor> GetPlaceholders();
        public abstract IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config);

        public virtual float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 420f;
        }

        public virtual string ProcessTemplateString(string templateString, ProcessDefBaseConfig config)
        {
            string rendered = ProcessDefPlaceholderUtility.ProcessTemplateString(templateString, GetPlaceholders(), ResolveValueByKey);
            return RenderFallbackTemplateKeys(rendered);
        }

        public virtual void DrawConfig(Rect rect, ProcessDefBaseConfig config)
        {
            TConfig typed = config as TConfig;
            if (typed == null)
            {
                Widgets.Label(rect, "RimTalkGenKnowledge.Message.InvalidProcessorConfigType".Translate(Id));
                return;
            }

            float y = rect.y;
            const float line = 24f;
            const float gap = 6f;
            string placeholderHint = ProcessDefPlaceholderUtility.BuildPlaceholderHint(GetPlaceholders());

            Rect enabledRect = new Rect(rect.x, y, rect.width, line);
            Widgets.CheckboxLabeled(enabledRect, "RimTalkGenKnowledge.Settings.Enabled".Translate(), ref typed.Enabled);
            y += line + gap;

            Rect includeModRect = new Rect(rect.x, y, rect.width, line);
            Widgets.CheckboxLabeled(includeModRect, "RimTalkGenKnowledge.Settings.IncludeModDefs".Translate(), ref typed.IncludeModDefs);
            y += line + gap;

            y = DrawTemplateRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.TagTemplate".Translate(), typed.TagTemplate, v => typed.TagTemplate = v);
            Rect tagHintRect = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(tagHintRect, placeholderHint);
            y += line + gap;

            y = DrawTemplateRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.KnowledgeTemplate".Translate(), typed.KnowledgeTemplate, v => typed.KnowledgeTemplate = v);
            Rect knowledgeHintRect = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(knowledgeHintRect, placeholderHint);
            y += line + gap;

            Rect resetRect = new Rect(rect.x, y, 120f, line);
            if (Widgets.ButtonText(resetRect, "RimTalkGenKnowledge.Settings.Reset".Translate()))
            {
                ApplyDefaultConfig(typed);
            }
            y += line + gap;

            Rect baseLabelRect = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(baseLabelRect, "RimTalkGenKnowledge.Settings.BaseImportance".Translate(typed.BaseImportance.ToString("0.00")));
            y += line;
            typed.BaseImportance = Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, line), typed.BaseImportance, -2f, 2f, false);
            y += line + gap;

            float oldMin = typed.ImportanceMin;
            float oldMax = typed.ImportanceMax;

            Rect minLabelRect = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(minLabelRect, $"{ "RimTalkGenKnowledge.Settings.ImportanceMin".Translate() }: {typed.ImportanceMin:0.00}");
            y += line;
            typed.ImportanceMin = Mathf.Clamp01(Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, line), typed.ImportanceMin, 0f, 1f, false));
            y += line + gap;

            Rect maxLabelRect = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(maxLabelRect, $"{ "RimTalkGenKnowledge.Settings.ImportanceMax".Translate() }: {typed.ImportanceMax:0.00}");
            y += line;
            typed.ImportanceMax = Mathf.Clamp01(Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, line), typed.ImportanceMax, 0f, 1f, false));
            y += line + gap;

            if (!Mathf.Approximately(typed.ImportanceMin, oldMin) && typed.ImportanceMin > typed.ImportanceMax)
            {
                typed.ImportanceMax = typed.ImportanceMin;
            }
            else if (!Mathf.Approximately(typed.ImportanceMax, oldMax) && typed.ImportanceMax < typed.ImportanceMin)
            {
                typed.ImportanceMin = typed.ImportanceMax;
            }

            y = DrawAdvancedConfig(rect.x, y, rect.width, line, gap, typed);
        }

        protected virtual float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, TConfig config)
        {
            return y;
        }

        protected void SetTemplateValues(Dictionary<string, string> values)
        {
            currentValues = values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected string RenderTag(TConfig config)
        {
            string template = string.IsNullOrWhiteSpace(config.TagTemplate) ? "{{label}}" : config.TagTemplate;
            return ProcessDefPlaceholderUtility.NormalizeTags(ProcessTemplateString(template, config));
        }

        protected string RenderContent(TConfig config)
        {
            string template = string.IsNullOrWhiteSpace(config.KnowledgeTemplate) ? "{{label}}: {{description}}" : config.KnowledgeTemplate;
            return ProcessTemplateString(template, config);
        }

        protected float ComputeFinalImportance(float raw, TConfig config)
        {
            return ProcessDefUtility.ClampImportance(raw, config);
        }

        private string ResolveValueByKey(string key)
        {
            if (currentValues == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return currentValues.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;
        }

        private string RenderFallbackTemplateKeys(string rendered)
        {
            if (string.IsNullOrEmpty(rendered) || currentValues == null || currentValues.Count == 0)
            {
                return rendered;
            }

            string result = rendered;
            foreach (KeyValuePair<string, string> entry in currentValues)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                string token = "{{" + entry.Key + "}}";
                result = result.Replace(token, entry.Value ?? string.Empty);
            }

            return result;
        }

        private float DrawTemplateRow(float x, float y, float width, float lineHeight, string label, string value, Action<string> setter)
        {
            const float labelWidth = 170f;
            const float buttonWidth = 62f;
            const float gap = 6f;

            Rect labelRect = new Rect(x, y, labelWidth, lineHeight);
            Widgets.Label(labelRect, label);

            float textWidth = width - labelWidth - buttonWidth - gap;
            if (textWidth < 40f)
            {
                textWidth = 40f;
            }

            Rect textRect = new Rect(labelRect.xMax, y, textWidth, lineHeight);
            string nextValue = Widgets.TextField(textRect, value ?? string.Empty);
            if (!string.Equals(nextValue, value, StringComparison.Ordinal))
            {
                setter(nextValue);
            }

            Rect insertRect = new Rect(textRect.xMax + gap, y, buttonWidth, lineHeight);
            if (Widgets.ButtonText(insertRect, "RimTalkGenKnowledge.Settings.Insert".Translate()))
            {
                ProcessDefPlaceholderUtility.ShowInsertPlaceholderMenu(GetPlaceholders(), setter, nextValue ?? string.Empty);
            }

            return y + lineHeight;
        }
    }
}
