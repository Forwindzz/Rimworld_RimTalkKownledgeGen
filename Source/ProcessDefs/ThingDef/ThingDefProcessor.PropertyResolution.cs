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
         static bool TryResolvePropertyValue(ThingDef def, string key, ThingPropertyDeviationConfig propertyConfig, out float value, out string valueText, out bool nonNegativeOnly, out bool baseValueIsZero)
        {
            value = 0f;
            valueText = "0";
            nonNegativeOnly = false;
            baseValueIsZero = false;

            switch (key)
            {
                case "market_value":
                    value = def.BaseMarketValue;
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "mass":
                    value = def.BaseMass;
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "max_hit_points":
                    value = ResolveMaxHitPoints(def);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "flammability":
                    if (!TryResolveStatOrStuffFactor(def, out value, "Flammability"))
                    {
                        return false;
                    }
                    valueText = (value * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "work_to_build":
                    if (!TryResolveStatBase(def, "WorkToBuild", out value))
                    {
                        return false;
                    }
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "beauty":
                    if (!TryResolveStatBase(def, "Beauty", out value))
                    {
                        return false;
                    }
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "cover_effectiveness":
                    if (!TryResolveStatBase(def, "CoverEffectiveness", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    value *= 100f;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "size_cells":
                    value = Mathf.Max(1, def.size.x * def.size.z);
                    valueText = value.ToString("0", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "power_w":
                    value = ResolvePowerConsumption(def);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "W";
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "nutrition":
                    value = ResolveNutrition(def);
                    valueText = value.ToString("0.###", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "medical_potency_pct":
                    if (!TryResolveStatBase(def, "MedicalPotency", out float medicalPotencyRaw))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(medicalPotencyRaw, 0f);
                    value = medicalPotencyRaw <= 2f ? medicalPotencyRaw * 100f : medicalPotencyRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "tend_quality_pct":
                    if (!TryResolveStatBase(def, "MedicalTendQualityMax", out float tendQualityRaw))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(tendQualityRaw, 0f);
                    value = tendQualityRaw <= 2f ? tendQualityRaw * 100f : tendQualityRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "armor_sharp_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float armorSharpRaw, "ArmorRating_Sharp", "StuffPower_Armor_Sharp"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(armorSharpRaw, 0f);
                    value = armorSharpRaw <= 2f ? armorSharpRaw * 100f : armorSharpRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "armor_blunt_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float armorBluntRaw, "ArmorRating_Blunt", "StuffPower_Armor_Blunt"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(armorBluntRaw, 0f);
                    value = armorBluntRaw <= 2f ? armorBluntRaw * 100f : armorBluntRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "armor_heat_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float armorHeatRaw, "ArmorRating_Heat", "StuffPower_Armor_Heat"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(armorHeatRaw, 0f);
                    value = armorHeatRaw <= 2f ? armorHeatRaw * 100f : armorHeatRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "insulation_cold_c":
                    if (!TryResolveStatOrStuffFactor(def, out value, "Insulation_Cold", "StuffPower_Insulation_Cold"))
                    {
                        return false;
                    }
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "C";
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "insulation_heat_c":
                    if (!TryResolveStatOrStuffFactor(def, out value, "Insulation_Heat", "StuffPower_Insulation_Heat"))
                    {
                        return false;
                    }
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "C";
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "sharp_damage_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float sharpDamageRaw, "SharpDamageMultiplier", "StuffPower_MeleeSharp"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(sharpDamageRaw, 0f);
                    value = sharpDamageRaw <= 2f ? sharpDamageRaw * 100f : sharpDamageRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "blunt_damage_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float bluntDamageRaw, "BluntDamageMultiplier", "StuffPower_MeleeBlunt"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(bluntDamageRaw, 0f);
                    value = bluntDamageRaw <= 2f ? bluntDamageRaw * 100f : bluntDamageRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "stuff_max_hit_points_pct":
                    if (!TryResolveStatOrStuffFactor(def, out float stuffHpRaw, "StuffPower_MaxHitPoints", "MaxHitPoints"))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(stuffHpRaw, 0f);
                    value = stuffHpRaw <= 2f ? stuffHpRaw * 100f : stuffHpRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "move_speed_factor":
                    if (!TryResolveStatBase(def, "MoveSpeedFactor", out float moveSpeedFactorRaw))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(moveSpeedFactorRaw, 0f);
                    value = moveSpeedFactorRaw - 1f;
                    valueText = (value * 100f).ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%";
                    return true;
                case "weapon_damage":
                    value = ResolveWeaponDamage(def);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "weapon_range":
                    value = ResolveWeaponRange(def);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    return true;
                case "weapon_accuracy_pct":
                    if (!TryResolveStatBase(def, "AccuracyTouch", out float accuracyTouchRaw))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(accuracyTouchRaw, 0f);
                    value = accuracyTouchRaw <= 2f ? accuracyTouchRaw * 100f : accuracyTouchRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "armor_penetration_pct":
                    if (!TryResolveStatBase(def, "ArmorPenetration", out float armorPenetrationRaw))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(armorPenetrationRaw, 0f);
                    value = armorPenetrationRaw <= 2f ? armorPenetrationRaw * 100f : armorPenetrationRaw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "comfort":
                    if (!TryResolveStatBase(def, "Comfort", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "work_to_make":
                    if (!TryResolveRecipeWorkAmount(def, out value))
                    {
                        return false;
                    }
                    if (value < 0f)
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "construction_skill_prerequisite":
                    value = Mathf.Max(0f, ProcessDefUtility.GetFloatMemberOrDefault(def, "constructionSkillPrerequisite", 0f));
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "weapon_accuracy_short_pct":
                    if (!TryResolveVerbPercent(def, "accuracyShort", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_accuracy_medium_pct":
                    if (!TryResolveVerbPercent(def, "accuracyMedium", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_accuracy_long_pct":
                    if (!TryResolveVerbPercent(def, "accuracyLong", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_burst_count":
                    if (!TryResolveVerbFloat(def, "burstShotCount", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "weapon_burst_interval_s":
                    if (!TryResolveVerbFloat(def, "ticksBetweenBurstShots", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value / 60f);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "s";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_warmup_s":
                    if (!TryResolveVerbFloat(def, "warmupTime", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "s";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_cooldown_s":
                    if (!TryResolveVerbFloat(def, "defaultCooldownTime", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "s";
                    nonNegativeOnly = true;
                    return true;
                case "weapon_suppression":
                    if (!TryResolveVerbFloat(def, "suppressionPower", out value))
                    {
                        return false;
                    }
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "turret_cooldown":
                    if (!TryResolveBuildingFloat(def, "turretBurstCooldownTime", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "s";
                    nonNegativeOnly = true;
                    return true;
                case "turret_warmup":
                    if (!TryResolveBuildingFloat(def, "turretBurstWarmupTime", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "s";
                    nonNegativeOnly = true;
                    return true;
                case "turret_burst_count":
                    if (!TryResolveBuildingFloat(def, "turretBurstShotCount", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "life_expectancy":
                    if (!TryResolveRaceFloat(def, "lifeExpectancy", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "gestation_days":
                    if (!TryResolveRaceFloat(def, "gestationPeriodDays", out value))
                    {
                        return false;
                    }
                    if (value <= 0f)
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + DayUnit();
                    nonNegativeOnly = true;
                    return true;
                case "grow_days":
                    if (!TryResolvePlantFloat(def, "growDays", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + DayUnit();
                    nonNegativeOnly = true;
                    return true;
                case "fertility_min_pct":
                    if (!TryResolvePlantFloat(def, "fertilityMin", out float fertilityRaw))
                    {
                        return false;
                    }
                    value = fertilityRaw <= 2f ? fertilityRaw * 100f : fertilityRaw;
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                case "milk_amount":
                    if (!TryResolveCompFloat(def, "milkable", "milkAmount", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "milk_interval_days":
                    if (!TryResolveCompFloat(def, "milkable", "milkIntervalDays", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + DayUnit();
                    nonNegativeOnly = true;
                    return true;
                case "wool_amount":
                    if (!TryResolveCompFloat(def, "shearable", "woolAmount", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                case "shear_interval_days":
                    if (!TryResolveCompFloat(def, "shearable", "shearIntervalDays", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + DayUnit();
                    nonNegativeOnly = true;
                    return true;
                case "egg_interval_days":
                    if (!TryResolveCompFloat(def, "egglayer", "eggLayIntervalDays", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + DayUnit();
                    nonNegativeOnly = true;
                    return true;
                case "egg_count_avg":
                    if (!TryResolveCompRangeAverage(def, "egglayer", "eggCountRange", out value))
                    {
                        return false;
                    }
                    value = Mathf.Max(0f, value);
                    baseValueIsZero = Mathf.Approximately(value, 0f);
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                    nonNegativeOnly = true;
                    return true;
                default:
                    return TryResolveGenericPropertyValue(def, key, propertyConfig, out value, out valueText, out nonNegativeOnly, out baseValueIsZero);
            }
        }

        private static bool TryResolveGenericPropertyValue(ThingDef def, string key, ThingPropertyDeviationConfig propertyConfig, out float value, out string valueText, out bool nonNegativeOnly, out bool baseValueIsZero)
        {
            value = 0f;
            valueText = "0";
            nonNegativeOnly = false;
            baseValueIsZero = false;

            if (def == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            foreach (string candidate in BuildPropertyCandidates(key))
            {
                if (!TryResolveStatBase(def, candidate, out float raw) && !TryResolveNumericMember(def, candidate, out raw))
                {
                    if (!TryResolveStuffStatFactor(def, candidate, out raw))
                    {
                        continue;
                    }
                }
                baseValueIsZero = Mathf.Approximately(raw, 0f);

                bool asPercent = (propertyConfig != null && propertyConfig.IsPercent) || key.EndsWith("_pct", StringComparison.OrdinalIgnoreCase);
                if (asPercent)
                {
                    if (raw <= 2f)
                    {
                        raw *= 100f;
                    }
                    value = raw;
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
                    nonNegativeOnly = true;
                    return true;
                }

                value = raw;
                if (key.EndsWith("_c", StringComparison.OrdinalIgnoreCase))
                {
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture) + "C";
                }
                else if (key.EndsWith("_w", StringComparison.OrdinalIgnoreCase))
                {
                    valueText = value.ToString("0.#", CultureInfo.InvariantCulture) + "W";
                }
                else
                {
                    valueText = value.ToString("0.##", CultureInfo.InvariantCulture);
                }

                nonNegativeOnly = value >= 0f;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> BuildPropertyCandidates(string key)
        {
            var candidates = new List<string>();
            string trimmed = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return candidates;
            }

            candidates.Add(trimmed);
            candidates.Add(ToPascalCaseToken(trimmed));

            if (trimmed.EndsWith("_pct", StringComparison.OrdinalIgnoreCase))
            {
                string baseKey = trimmed.Substring(0, trimmed.Length - 4);
                candidates.Add(baseKey);
                candidates.Add(ToPascalCaseToken(baseKey));
            }

            if (trimmed.EndsWith("_c", StringComparison.OrdinalIgnoreCase) || trimmed.EndsWith("_w", StringComparison.OrdinalIgnoreCase))
            {
                string baseKey = trimmed.Substring(0, trimmed.Length - 2);
                candidates.Add(baseKey);
                candidates.Add(ToPascalCaseToken(baseKey));
            }

            return candidates.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string ToPascalCaseToken(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string[] parts = key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return key;
            }

            var buffer = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }
                string lower = part.ToLowerInvariant();
                buffer.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                {
                    buffer.Append(lower.Substring(1));
                }
            }
            return buffer.ToString();
        }

        private static bool TryResolveStatBase(ThingDef def, string statDefName, out float value)
        {
            value = 0f;
            if (def?.statBases == null || string.IsNullOrWhiteSpace(statDefName))
            {
                return false;
            }

            for (int i = 0; i < def.statBases.Count; i++)
            {
                StatModifier modifier = def.statBases[i];
                if (modifier?.stat != null && string.Equals(modifier.stat.defName, statDefName, StringComparison.OrdinalIgnoreCase))
                {
                    value = modifier.value;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveStatOrStuffFactor(ThingDef def, out float value, params string[] statDefNames)
        {
            value = 0f;
            if (statDefNames == null || statDefNames.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < statDefNames.Length; i++)
            {
                string name = statDefNames[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (TryResolveStatBase(def, name, out value))
                {
                    return true;
                }

                if (TryResolveStuffStatFactor(def, name, out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveStuffStatFactor(ThingDef def, string statDefName, out float value)
        {
            value = 0f;
            if (def == null || string.IsNullOrWhiteSpace(statDefName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "stuffProps", out object stuffProps) || stuffProps == null)
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(stuffProps, "statFactors", out object factorsObj) || factorsObj == null)
            {
                return false;
            }

            if (!(factorsObj is System.Collections.IList factors))
            {
                return false;
            }

            for (int i = 0; i < factors.Count; i++)
            {
                object item = factors[i];
                if (item == null)
                {
                    continue;
                }

                if (!ProcessDefUtility.TryGetMemberValue(item, "stat", out object statObj) || statObj == null)
                {
                    continue;
                }

                string defName = ProcessDefUtility.GetStringMemberOrDefault(statObj, "defName", string.Empty);
                if (!string.Equals(defName, statDefName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = ProcessDefUtility.GetFloatMemberOrDefault(item, "value", 0f);
                return true;
            }

            return false;
        }

        private static bool TryResolveNumericMember(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (!ProcessDefUtility.TryGetMemberValue(def, memberName, out object raw) || raw == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToSingle(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float ResolveMaxHitPoints(ThingDef def)
        {
            float hp = ProcessDefUtility.GetFloatMemberOrDefault(def, "BaseMaxHitPoints", 0f);
            if (hp <= 0f)
            {
                hp = ProcessDefUtility.GetFloatMemberOrDefault(def, "baseHitPoints", 0f);
            }
            if (hp <= 0f)
            {
                hp = ResolveStatBase(def, "MaxHitPoints");
            }
            return Mathf.Max(0f, hp);
        }

        private static float ResolveStatBase(ThingDef def, string statDefName)
        {
            if (def?.statBases == null || string.IsNullOrWhiteSpace(statDefName))
            {
                return 0f;
            }

            for (int i = 0; i < def.statBases.Count; i++)
            {
                StatModifier modifier = def.statBases[i];
                if (modifier?.stat != null && string.Equals(modifier.stat.defName, statDefName, StringComparison.OrdinalIgnoreCase))
                {
                    return modifier.value;
                }
            }

            return 0f;
        }

        private static float ResolvePercentStat(ThingDef def, string statDefName)
        {
            float raw = ResolveStatBase(def, statDefName);
            if (raw <= 2f)
            {
                raw *= 100f;
            }
            return raw;
        }

        private static float ResolvePowerConsumption(ThingDef def)
        {
            if (def?.comps == null)
            {
                return 0f;
            }
            for (int i = 0; i < def.comps.Count; i++)
            {
                object comp = def.comps[i];
                if (comp == null)
                {
                    continue;
                }
                string compClassName = ProcessDefUtility.GetStringMemberOrDefault(comp, "compClass", string.Empty);
                if (string.IsNullOrWhiteSpace(compClassName) || !compClassName.Contains("CompPowerTrader"))
                {
                    continue;
                }
                float watts = ProcessDefUtility.GetFloatMemberOrDefault(comp, "basePowerConsumption", 0f);
                if (Mathf.Approximately(watts, 0f))
                {
                    watts = ProcessDefUtility.GetFloatMemberOrDefault(comp, "PowerConsumption", 0f);
                }
                return Mathf.Abs(watts);
            }
            return 0f;
        }

        private static float ResolveNutrition(ThingDef def)
        {
            if (ProcessDefUtility.TryGetMemberValue(def, "ingestible", out object ingestible) && ingestible != null)
            {
                float cached = ProcessDefUtility.GetFloatMemberOrDefault(ingestible, "CachedNutrition", 0f);
                if (cached > 0f)
                {
                    return cached;
                }
                float baseNutrition = ProcessDefUtility.GetFloatMemberOrDefault(ingestible, "baseNutrition", 0f);
                if (baseNutrition > 0f)
                {
                    return baseNutrition;
                }
            }
            return ResolveStatBase(def, "Nutrition");
        }

        private static float ResolveWeaponDamage(ThingDef def)
        {
            if (def?.tools != null && def.tools.Count > 0)
            {
                float sum = 0f;
                int count = 0;
                for (int i = 0; i < def.tools.Count; i++)
                {
                    object tool = def.tools[i];
                    float power = ProcessDefUtility.GetFloatMemberOrDefault(tool, "power", 0f);
                    if (power > 0f)
                    {
                        sum += power;
                        count++;
                    }
                }
                if (count > 0)
                {
                    return sum / count;
                }
            }
            return ResolveStatBase(def, "MeleeWeapon_AverageDPS");
        }

        private static float ResolveWeaponRange(ThingDef def)
        {
            if (def == null)
            {
                return 0f;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "verbs", out object verbsObj) && verbsObj is System.Collections.IList verbs && verbs.Count > 0)
            {
                float maxRange = 0f;
                for (int i = 0; i < verbs.Count; i++)
                {
                    object verb = verbs[i];
                    float range = ProcessDefUtility.GetFloatMemberOrDefault(verb, "range", 0f);
                    if (range > maxRange)
                    {
                        maxRange = range;
                    }
                }
                return maxRange;
            }
            return 0f;
        }

        private static bool TryResolveBuildingFloat(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (def == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "building", out object building) || building == null)
            {
                return false;
            }

            return TryResolveFloatMember(building, memberName, out value);
        }

        private static bool TryResolvePlantFloat(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (def == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "plant", out object plant) || plant == null)
            {
                return false;
            }

            return TryResolveFloatMember(plant, memberName, out value);
        }

        private static bool TryResolveRaceFloat(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (def == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "race", out object race) || race == null)
            {
                return false;
            }

            return TryResolveFloatMember(race, memberName, out value);
        }

        private static bool TryResolveRecipeWorkAmount(ThingDef def, out float value)
        {
            value = 0f;
            if (def == null)
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "recipeMaker", out object recipeMaker) || recipeMaker == null)
            {
                return false;
            }

            return TryResolveFloatMember(recipeMaker, "workAmount", out value);
        }

        private static bool TryResolveVerbPercent(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (!TryResolveVerbFloat(def, memberName, out float raw))
            {
                return false;
            }

            value = raw <= 2f ? raw * 100f : raw;
            return true;
        }

        private static bool TryResolveVerbFloat(ThingDef def, string memberName, out float value)
        {
            value = 0f;
            if (def == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(def, "verbs", out object verbsObj) || !(verbsObj is System.Collections.IList verbs) || verbs.Count == 0)
            {
                return false;
            }

            bool found = false;
            float max = float.MinValue;
            for (int i = 0; i < verbs.Count; i++)
            {
                object verb = verbs[i];
                if (verb == null)
                {
                    continue;
                }

                if (!TryResolveFloatMember(verb, memberName, out float v))
                {
                    continue;
                }

                found = true;
                if (v > max)
                {
                    max = v;
                }
            }

            if (!found)
            {
                return false;
            }

            value = max;
            return true;
        }

        private static bool TryResolveCompFloat(ThingDef def, string compToken, string memberName, out float value)
        {
            value = 0f;
            if (def?.comps == null || string.IsNullOrWhiteSpace(compToken) || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            string token = compToken.ToLowerInvariant();
            for (int i = 0; i < def.comps.Count; i++)
            {
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

                if (string.IsNullOrWhiteSpace(compName) || !compName.ToLowerInvariant().Contains(token))
                {
                    continue;
                }

                if (TryResolveFloatMember(comp, memberName, out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveCompRangeAverage(ThingDef def, string compToken, string rangeMemberName, out float value)
        {
            value = 0f;
            if (def?.comps == null || string.IsNullOrWhiteSpace(compToken) || string.IsNullOrWhiteSpace(rangeMemberName))
            {
                return false;
            }

            string token = compToken.ToLowerInvariant();
            for (int i = 0; i < def.comps.Count; i++)
            {
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

                if (string.IsNullOrWhiteSpace(compName) || !compName.ToLowerInvariant().Contains(token))
                {
                    continue;
                }

                if (!ProcessDefUtility.TryGetMemberValue(comp, rangeMemberName, out object rangeObj) || rangeObj == null)
                {
                    continue;
                }

                if (TryResolveRangeAverage(rangeObj, out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveRangeAverage(object rangeObj, out float value)
        {
            value = 0f;
            if (rangeObj == null)
            {
                return false;
            }

            bool hasMin = TryResolveFloatMember(rangeObj, "min", out float min);
            bool hasMax = TryResolveFloatMember(rangeObj, "max", out float max);
            if (hasMin && hasMax)
            {
                value = (min + max) * 0.5f;
                return true;
            }

            if (TryResolveFloatMember(rangeObj, "TrueAverage", out float avg))
            {
                value = avg;
                return true;
            }

            return false;
        }

        private static bool TryResolveFloatMember(object target, string memberName, out float value)
        {
            value = 0f;
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            if (!ProcessDefUtility.TryGetMemberValue(target, memberName, out object raw) || raw == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToSingle(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

