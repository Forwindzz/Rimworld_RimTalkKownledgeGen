using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefUiUtility
    {
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
