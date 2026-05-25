using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefUtility
    {
        public const float InternalLogFloor = 1f;

        public static float SafeLog10(float value)
        {
            return (float)Math.Log10(Math.Max(value, InternalLogFloor));
        }

        public static float ClampImportance(float value, ProcessDefBaseConfig config)
        {
            float min = config?.ImportanceMin ?? 0f;
            float max = config?.ImportanceMax ?? 1f;
            if (max < min)
            {
                float tmp = min;
                min = max;
                max = tmp;
            }

            if (min < 0f)
            {
                min = 0f;
            }
            if (max > 1f)
            {
                max = 1f;
            }

            return value < min ? min : (value > max ? max : value);
        }

        public static bool ShouldIncludeDef(Def def, bool includeModDefs)
        {
            if (def == null)
            {
                return false;
            }

            if (includeModDefs)
            {
                return true;
            }

            return !IsExternalModDef(def);
        }

        public static bool IsExternalModDef(Def def)
        {
            if (def == null)
            {
                return false;
            }

            object pack = def.modContentPack;
            if (pack == null)
            {
                return false;
            }

            string packageId = ReadPackageId(pack);
            if (string.IsNullOrWhiteSpace(packageId))
            {
                // Unknown source: treat as non-external to avoid accidental data loss.
                return false;
            }

            return !packageId.StartsWith("ludeon.rimworld", StringComparison.OrdinalIgnoreCase);
        }

        public static string ReadPackageId(object modContentPack)
        {
            if (modContentPack == null)
            {
                return null;
            }

            Type type = modContentPack.GetType();
            string[] candidates =
            {
                "PackageIdPlayerFacing",
                "PackageId",
                "PackageIdNonUnique"
            };

            foreach (string name in candidates)
            {
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    continue;
                }

                object value = property.GetValue(modContentPack, null);
                if (value == null)
                {
                    continue;
                }

                string text = value.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim().ToLowerInvariant();
                }
            }

            return null;
        }

        public static string TrimOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        public static int BoolMetric(bool value)
        {
            return value ? 1 : 0;
        }

        public static float ParseFloat(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return fallback;
        }

        public static Dictionary<string, float> ParseKeyFloatMap(string mapText, Dictionary<string, float> fallback)
        {
            if (string.IsNullOrWhiteSpace(mapText))
            {
                return fallback ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            }

            var map = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            string[] pairs = mapText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] segments = pair.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length != 2)
                {
                    continue;
                }

                string key = segments[0].Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!float.TryParse(segments[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float score))
                {
                    continue;
                }

                map[key] = score;
            }

            if (map.Count == 0)
            {
                return fallback ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            }

            return map;
        }

        public static string GetDefDescription(Def def)
        {
            return TrimOrNull(def?.description);
        }

        public static bool TryGetMemberValue(object target, string memberName, out object value)
        {
            value = null;
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            Type type = target.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                value = field.GetValue(target);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                value = property.GetValue(target, null);
                return true;
            }

            return false;
        }

        public static bool GetBoolMemberOrDefault(object target, string memberName, bool fallback)
        {
            if (!TryGetMemberValue(target, memberName, out object value) || value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        public static float GetFloatMemberOrDefault(object target, string memberName, float fallback)
        {
            if (!TryGetMemberValue(target, memberName, out object value) || value == null)
            {
                return fallback;
            }

            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        public static string GetStringMemberOrDefault(object target, string memberName, string fallback)
        {
            if (!TryGetMemberValue(target, memberName, out object value) || value == null)
            {
                return fallback;
            }

            return value.ToString() ?? fallback;
        }
    }
}
