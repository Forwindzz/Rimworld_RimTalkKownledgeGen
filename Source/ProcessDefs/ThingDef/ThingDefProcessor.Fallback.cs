using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public partial class ThingDefProcessor
    {
         static IEnumerable<string> BuildFallbackAttributes(ThingDef def, ThingProcessDefConfig config)
        {
            var result = new List<string>();
            HashSet<string> exclude = BuildFilter(config.FallbackAttributeExcludeKeys);
            int maxLines = config != null && config.DebugForceShowDeviation
                ? 64
                : config?.FallbackAttributeMaxLines ?? 6;
            AppendIfAllowed(result, exclude, maxLines, "thingClass", def.thingClass?.Name);
            AppendIfAllowed(result, exclude, maxLines, "passability", def.passability.ToString());
            AppendIfAllowed(result, exclude, maxLines, "pathCost", def.pathCost.ToString(CultureInfo.InvariantCulture));
            AppendIfAllowed(result, exclude, maxLines, "useHitPoints", def.useHitPoints ? "true" : "false");
            AppendIfAllowed(result, exclude, maxLines, "madeFromStuff", def.MadeFromStuff ? "true" : "false");
            AppendStatBaseAttributes(result, exclude, maxLines, def);
            AppendStuffFactorAttributes(result, exclude, maxLines, def);
            AppendComponentAttributes(result, exclude, maxLines, def);
            return result;
        }

        private static void AppendStatBaseAttributes(List<string> output, HashSet<string> exclude, int maxLines, ThingDef def)
        {
            if (def?.statBases == null)
            {
                return;
            }

            for (int i = 0; i < def.statBases.Count; i++)
            {
                if (output.Count >= Math.Max(0, maxLines))
                {
                    return;
                }

                StatModifier modifier = def.statBases[i];
                if (modifier?.stat == null || Mathf.Approximately(modifier.value, 0f))
                {
                    continue;
                }

                string key = "stat." + modifier.stat.defName;
                string value = modifier.value.ToString("0.###", CultureInfo.InvariantCulture);
                AppendIfAllowed(output, exclude, maxLines, key, value);
            }
        }

        private static void AppendStuffFactorAttributes(List<string> output, HashSet<string> exclude, int maxLines, ThingDef def)
        {
            if (output.Count >= Math.Max(0, maxLines))
            {
                return;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "stuffProps", out object stuffProps) || stuffProps == null)
            {
                return;
            }

            if (!ProcessDefUtility.TryGetMemberValue(stuffProps, "statFactors", out object factorsObj) || !(factorsObj is System.Collections.IList factors))
            {
                return;
            }

            for (int i = 0; i < factors.Count; i++)
            {
                if (output.Count >= Math.Max(0, maxLines))
                {
                    return;
                }

                object factor = factors[i];
                if (factor == null)
                {
                    continue;
                }

                if (!ProcessDefUtility.TryGetMemberValue(factor, "stat", out object statObj) || statObj == null)
                {
                    continue;
                }

                string statName = ProcessDefUtility.GetStringMemberOrDefault(statObj, "defName", string.Empty);
                if (string.IsNullOrWhiteSpace(statName))
                {
                    continue;
                }

                float value = ProcessDefUtility.GetFloatMemberOrDefault(factor, "value", 0f);
                if (Mathf.Approximately(value, 0f))
                {
                    continue;
                }

                AppendIfAllowed(
                    output,
                    exclude,
                    maxLines,
                    "stuffFactor." + statName,
                    value.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static void AppendComponentAttributes(List<string> output, HashSet<string> exclude, int maxLines, ThingDef def)
        {
            if (def?.comps == null || output.Count >= Math.Max(0, maxLines))
            {
                return;
            }

            for (int i = 0; i < def.comps.Count; i++)
            {
                if (output.Count >= Math.Max(0, maxLines))
                {
                    return;
                }

                object comp = def.comps[i];
                if (comp == null)
                {
                    continue;
                }

                string compName = ProcessDefUtility.GetStringMemberOrDefault(comp, "compClass", string.Empty);
                if (string.IsNullOrWhiteSpace(compName))
                {
                    compName = comp.GetType().Name;
                }
                compName = compName.Split('.').LastOrDefault() ?? compName;
                AppendCompKnownAttributes(output, exclude, maxLines, compName, comp);
            }
        }

        private static void AppendCompKnownAttributes(List<string> output, HashSet<string> exclude, int maxLines, string compName, object comp)
        {
            string lowered = (compName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(lowered))
            {
                return;
            }

            if (lowered.Contains("powertrader") || lowered.Contains("powerplant"))
            {
                AppendCompFloat(output, exclude, maxLines, compName, comp, "basePowerConsumption");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "PowerConsumption");
                AppendCompBool(output, exclude, maxLines, compName, comp, "transmitsPower");
                AppendCompBool(output, exclude, maxLines, compName, comp, "shortCircuitInRain");
            }

            if (lowered.Contains("refuelable") || lowered.Contains("fuelable"))
            {
                AppendCompFloat(output, exclude, maxLines, compName, comp, "fuelCapacity");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "fuelConsumptionRate");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "initialFuelPercent");
                AppendCompBool(output, exclude, maxLines, compName, comp, "consumeFuelOnlyWhenUsed");
            }

            if (lowered.Contains("rottable"))
            {
                AppendCompFloat(output, exclude, maxLines, compName, comp, "daysToRotStart");
                AppendCompBool(output, exclude, maxLines, compName, comp, "rotDestroys");
            }

            if (lowered.Contains("explosive"))
            {
                AppendCompFloat(output, exclude, maxLines, compName, comp, "explosiveRadius");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "explosiveDamageAmountBase");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "damageAmountBase");
                AppendCompBool(output, exclude, maxLines, compName, comp, "explodeOnKilled");
            }

            if (lowered.Contains("battery"))
            {
                AppendCompFloat(output, exclude, maxLines, compName, comp, "storedEnergyMax");
                AppendCompFloat(output, exclude, maxLines, compName, comp, "efficiency");
                AppendCompBool(output, exclude, maxLines, compName, comp, "drawPower");
            }

            if (lowered.Contains("flickable"))
            {
                AppendCompBool(output, exclude, maxLines, compName, comp, "switchIsOn");
            }

            if (lowered.Contains("forbiddable"))
            {
                AppendCompBool(output, exclude, maxLines, compName, comp, "alwaysForbidden");
            }

            if (lowered.Contains("quality"))
            {
                AppendCompBool(output, exclude, maxLines, compName, comp, "generateQuality");
            }

            if (lowered.Contains("equippable"))
            {
                AppendCompBool(output, exclude, maxLines, compName, comp, "isWeapon");
            }
        }

        private static void AppendCompFloat(List<string> output, HashSet<string> exclude, int maxLines, string compName, object comp, string memberName)
        {
            float value = ProcessDefUtility.GetFloatMemberOrDefault(comp, memberName, 0f);
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            AppendIfAllowed(
                output,
                exclude,
                maxLines,
                "comp." + compName + "." + memberName,
                value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AppendCompBool(List<string> output, HashSet<string> exclude, int maxLines, string compName, object comp, string memberName)
        {
            bool value = ProcessDefUtility.GetBoolMemberOrDefault(comp, memberName, false);
            if (!value)
            {
                return;
            }

            AppendIfAllowed(output, exclude, maxLines, "comp." + compName + "." + memberName, "true");
        }

        private static void AppendIfAllowed(List<string> output, HashSet<string> exclude, int maxLines, string key, string value)
        {
            if (output.Count >= Math.Max(0, maxLines))
            {
                return;
            }
            if (exclude.Contains(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            output.Add(key + "=" + value);
        }
    }
}

