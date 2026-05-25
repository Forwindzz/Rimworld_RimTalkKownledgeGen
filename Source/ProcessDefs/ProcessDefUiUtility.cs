using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefUiUtility
    {
        public static float DrawCommonBase(Rect rect, ProcessDefBaseConfig config, Action drawAdvanced)
        {
            float y = rect.y;
            const float line = 24f;
            const float gap = 6f;

            Rect enabledRect = new Rect(rect.x, y, rect.width, line);
            Widgets.CheckboxLabeled(enabledRect, "RimTalkGenKnowledge.Settings.Enabled".Translate(), ref config.Enabled);
            y += line + gap;

            Rect includeModRect = new Rect(rect.x, y, rect.width, line);
            Widgets.CheckboxLabeled(includeModRect, "RimTalkGenKnowledge.Settings.IncludeModDefs".Translate(), ref config.IncludeModDefs);
            y += line + gap;

            y = DrawTextRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.TagTemplate".Translate(), config.TagTemplate, v => config.TagTemplate = v);
            y += gap;
            y = DrawTextRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.KnowledgeTemplate".Translate(), config.KnowledgeTemplate, v => config.KnowledgeTemplate = v);
            y += gap;

            Rect baseLabel = new Rect(rect.x, y, rect.width, line);
            Widgets.Label(baseLabel, "RimTalkGenKnowledge.Settings.BaseImportance".Translate(config.BaseImportance.ToString("0.00", CultureInfo.InvariantCulture)));
            y += line;
            config.BaseImportance = Widgets.HorizontalSlider(new Rect(rect.x, y, rect.width, line), config.BaseImportance, -2f, 2f, false);
            y += line + gap;

            y = DrawFloatRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.ImportanceMin".Translate(), config.ImportanceMin, v => config.ImportanceMin = v);
            y += gap;
            y = DrawFloatRow(rect.x, y, rect.width, line, "RimTalkGenKnowledge.Settings.ImportanceMax".Translate(), config.ImportanceMax, v => config.ImportanceMax = v);
            y += gap;

            drawAdvanced?.Invoke();
            return y;
        }

        public static float DrawTextRow(float x, float y, float width, float lineHeight, string label, string value, Action<string> setter)
        {
            const float labelWidth = 170f;
            Rect labelRect = new Rect(x, y, labelWidth, lineHeight);
            Widgets.Label(labelRect, label);

            Rect textRect = new Rect(labelRect.xMax, y, width - labelWidth, lineHeight);
            string next = Widgets.TextField(textRect, value ?? string.Empty);
            if (!string.Equals(next, value, StringComparison.Ordinal))
            {
                setter(next);
            }

            return y + lineHeight;
        }

        public static float DrawFloatRow(float x, float y, float width, float lineHeight, string label, float value, Action<float> setter)
        {
            const float labelWidth = 170f;
            Rect labelRect = new Rect(x, y, labelWidth, lineHeight);
            Widgets.Label(labelRect, label);

            Rect textRect = new Rect(labelRect.xMax, y, width - labelWidth, lineHeight);
            string next = Widgets.TextField(textRect, value.ToString("0.####", CultureInfo.InvariantCulture));
            float parsed = ProcessDefUtility.ParseFloat(next, value);
            setter(parsed);

            return y + lineHeight;
        }

        public static float DrawIntRow(float x, float y, float width, float lineHeight, string label, int value, Action<int> setter)
        {
            const float labelWidth = 170f;
            Rect labelRect = new Rect(x, y, labelWidth, lineHeight);
            Widgets.Label(labelRect, label);

            Rect textRect = new Rect(labelRect.xMax, y, width - labelWidth, lineHeight);
            string next = Widgets.TextField(textRect, value.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(next, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                setter(parsed);
            }

            return y + lineHeight;
        }
    }
}
