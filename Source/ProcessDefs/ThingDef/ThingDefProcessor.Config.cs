using System;
using System.Collections.Generic;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public partial class ThingDefProcessor
    {
         static HashSet<string> BuildFilter(string commaSeparated)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(commaSeparated))
            {
                return set;
            }
            string normalized = commaSeparated.Replace('，', ',').Replace('、', ',').Replace('；', ',');
            string[] tokens = normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string value = tokens[i].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    set.Add(value);
                }
            }
            return set;
        }

        private static Dictionary<string, ThingCategoryRuleConfig> CloneCategoryRules(Dictionary<string, ThingCategoryRuleConfig> source)
        {
            var copy = new Dictionary<string, ThingCategoryRuleConfig>(StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return copy;
            }
            foreach (var pair in source)
            {
                ThingCategoryRuleConfig item = pair.Value ?? new ThingCategoryRuleConfig();
                copy[pair.Key] = new ThingCategoryRuleConfig { Enabled = item.Enabled, PropertyKeys = item.PropertyKeys, MaxLines = item.MaxLines };
            }
            return copy;
        }

        private static Dictionary<string, ThingPropertyDeviationConfig> ClonePropertyConfigs(Dictionary<string, ThingPropertyDeviationConfig> source)
        {
            var copy = new Dictionary<string, ThingPropertyDeviationConfig>(StringComparer.OrdinalIgnoreCase);
            if (source == null)
            {
                return copy;
            }
            foreach (var pair in source)
            {
                ThingPropertyDeviationConfig item = pair.Value ?? new ThingPropertyDeviationConfig();
                copy[pair.Key] = new ThingPropertyDeviationConfig
                {
                    Enabled = item.Enabled,
                    RangeMin = item.RangeMin,
                    RangeMax = item.RangeMax,
                    Scale = item.Scale,
                    NonNegativeOnly = item.NonNegativeOnly,
                    IsPercent = item.IsPercent,
                    DisplayName = item.DisplayName,
                    StageTextNegStrong = item.StageTextNegStrong,
                    StageTextNegLight = item.StageTextNegLight,
                    StageTextPosLight = item.StageTextPosLight,
                    StageTextPosStrong = item.StageTextPosStrong
                };
            }
            return copy;
        }

        private static void EnsureDefaults(ThingProcessDefConfig config)
        {
            if (config.CategoryRules == null)
            {
                config.CategoryRules = new Dictionary<string, ThingCategoryRuleConfig>(StringComparer.OrdinalIgnoreCase);
            }
            if (config.PropertyDeviationConfigs == null)
            {
                config.PropertyDeviationConfigs = new Dictionary<string, ThingPropertyDeviationConfig>(StringComparer.OrdinalIgnoreCase);
            }

            EnsureCategoryRule(config, KindBuilding, "max_hit_points,comfort,flammability,work_to_build,beauty,cover_effectiveness,size_cells,power_w,construction_skill_prerequisite,turret_cooldown,turret_warmup", 6);
            EnsureCategoryRule(config, KindItem, "market_value,max_hit_points,mass,comfort,armor_sharp_pct,armor_blunt_pct,armor_heat_pct,insulation_cold_c,insulation_heat_c,sharp_damage_pct,blunt_damage_pct,stuff_max_hit_points_pct", 6);
            EnsureCategoryRule(config, KindFood, "nutrition,market_value,grow_days,fertility_min_pct,gestation_days", 4);
            EnsureCategoryRule(config, KindMedicine, "medical_potency_pct,tend_quality_pct,market_value", 3);
            EnsureCategoryRule(config, KindApparel, "comfort,armor_sharp_pct,armor_blunt_pct,armor_heat_pct,insulation_cold_c,insulation_heat_c,move_speed_factor", 5);
            EnsureCategoryRule(config, KindWeapon, "weapon_damage,weapon_range,weapon_accuracy_pct,weapon_accuracy_short_pct,weapon_accuracy_medium_pct,weapon_accuracy_long_pct,weapon_burst_count,weapon_burst_interval_s,weapon_warmup_s,weapon_cooldown_s,armor_penetration_pct", 6);
            EnsureProperty(config, "market_value", T("RimTalkGenKnowledge.Text.Thing.Prop.market_value.Display"), 0f, 400f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.market_value.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.market_value.High"));
            if (config.PropertyDeviationConfigs.TryGetValue("market_value", out ThingPropertyDeviationConfig marketValueCfg) &&
                marketValueCfg != null &&
                Math.Abs(marketValueCfg.RangeMin - 10f) < 0.0001f &&
                Math.Abs(marketValueCfg.RangeMax - 400f) < 0.0001f)
            {
                // migrate previous default [10,400] -> new default [0,400]
                marketValueCfg.RangeMin = 0f;
            }
            EnsureProperty(config, "mass", T("RimTalkGenKnowledge.Text.Thing.Prop.mass.Display"), 0f, 3f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.mass.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.mass.High"));
            if (config.PropertyDeviationConfigs.TryGetValue("mass", out ThingPropertyDeviationConfig massCfg) &&
                massCfg != null &&
                Math.Abs(massCfg.RangeMin - 0.1f) < 0.0001f &&
                Math.Abs(massCfg.RangeMax - 10f) < 0.0001f)
            {
                // migrate previous default [0.1,10] -> new default [0,3]
                massCfg.RangeMin = 0f;
                massCfg.RangeMax = 3f;
            }
            EnsureProperty(config, "max_hit_points", T("RimTalkGenKnowledge.Text.Thing.Prop.max_hit_points.Display"), 20f, 300f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.max_hit_points.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.max_hit_points.High"));
            EnsureProperty(config, "comfort", T("RimTalkGenKnowledge.Text.Thing.Prop.comfort.Display"), 0.5f, 1.0f, true, false, T("RimTalkGenKnowledge.Text.Thing.Stage.NegLight"), T("RimTalkGenKnowledge.Text.Thing.Stage.PosLight"));
            EnsureProperty(config, "flammability", T("RimTalkGenKnowledge.Text.Thing.Prop.flammability.Display"), 0.1f, 0.5f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.flammability.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.flammability.High"), 1f, false);
            EnsureProperty(config, "work_to_build", T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_build.Display"), 500f, 5000f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_build.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_build.High"));
            EnsureProperty(config, "work_to_make", T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_make.Display"), 10f, 500f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_make.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.work_to_make.High"));
            EnsureProperty(config, "construction_skill_prerequisite", T("RimTalkGenKnowledge.Text.Thing.Prop.construction_skill_prerequisite.Display"), 0f, 3f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.construction_skill_prerequisite.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.construction_skill_prerequisite.High"));
            EnsureProperty(config, "beauty", T("RimTalkGenKnowledge.Text.Thing.Prop.beauty.Display"), -5f, 5f, false, false, T("RimTalkGenKnowledge.Text.Thing.Prop.beauty.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.beauty.High"), 0.1f);
            EnsureProperty(config, "cover_effectiveness", T("RimTalkGenKnowledge.Text.Thing.Prop.cover_effectiveness.Display"), 0f, 50f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.cover_effectiveness.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.cover_effectiveness.High"), 2f);
            EnsureProperty(config, "size_cells", T("RimTalkGenKnowledge.Text.Thing.Prop.size_cells.Display"), 0f, 6f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.size_cells.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.size_cells.High"));
            EnsureProperty(config, "power_w", T("RimTalkGenKnowledge.Text.Thing.Prop.power_w.Display"), 30f, 300f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.power_w.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.power_w.High"));
            EnsureProperty(config, "nutrition", T("RimTalkGenKnowledge.Text.Thing.Prop.nutrition.Display"), 0.3f, 1.0f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.nutrition.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.nutrition.High"));
            EnsureProperty(config, "grow_days", T("RimTalkGenKnowledge.Text.Thing.Prop.grow_days.Display"), 4f, 7f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.grow_days.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.grow_days.High"));
            EnsureProperty(config, "fertility_min_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.fertility_min_pct.Display"), 65f, 105f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.fertility_min_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.fertility_min_pct.High"));
            EnsureProperty(config, "life_expectancy", T("RimTalkGenKnowledge.Text.Thing.Prop.life_expectancy.Display"), 10f, 80f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.life_expectancy.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.life_expectancy.High"));
            EnsureProperty(config, "gestation_days", T("RimTalkGenKnowledge.Text.Thing.Prop.gestation_days.Display"), 2f, 20f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.gestation_days.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.gestation_days.High"));
            EnsureProperty(config, "milk_amount", T("RimTalkGenKnowledge.Text.Thing.Prop.milk_amount.Display"), 5f, 30f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.milk_amount.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.milk_amount.High"));
            EnsureProperty(config, "milk_interval_days", T("RimTalkGenKnowledge.Text.Thing.Prop.milk_interval_days.Display"), 1f, 5f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.interval.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.interval.High"));
            EnsureProperty(config, "wool_amount", T("RimTalkGenKnowledge.Text.Thing.Prop.wool_amount.Display"), 20f, 120f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.wool_amount.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.wool_amount.High"));
            EnsureProperty(config, "shear_interval_days", T("RimTalkGenKnowledge.Text.Thing.Prop.shear_interval_days.Display"), 5f, 30f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.interval.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.interval.High"));
            EnsureProperty(config, "egg_interval_days", T("RimTalkGenKnowledge.Text.Thing.Prop.egg_interval_days.Display"), 1f, 10f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.interval.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.interval.High"));
            EnsureProperty(config, "egg_count_avg", T("RimTalkGenKnowledge.Text.Thing.Prop.egg_count_avg.Display"), 1f, 4f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.egg_count_avg.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.egg_count_avg.High"));
            EnsureProperty(config, "medical_potency_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.medical_potency_pct.Display"), 90f, 110f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.medical_potency_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.medical_potency_pct.High"));
            EnsureProperty(config, "tend_quality_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.tend_quality_pct.Display"), 90f, 110f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.tend_quality_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.tend_quality_pct.High"));
            EnsureProperty(config, "armor_sharp_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.armor_sharp_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.armor.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.armor.High"));
            EnsureProperty(config, "armor_blunt_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.armor_blunt_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.armor.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.armor.High"));
            EnsureProperty(config, "armor_heat_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.armor_heat_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.armor.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.armor.High"));
            EnsureProperty(config, "insulation_cold_c", T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_cold_c.Display"), 0f, 13f, false, false, T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_cold_c.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_cold_c.High"), 0.5f);
            EnsureProperty(config, "insulation_heat_c", T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_heat_c.Display"), 0f, 13f, false, false, T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_heat_c.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.insulation_heat_c.High"), 0.5f);
            EnsureProperty(config, "move_speed_factor", T("RimTalkGenKnowledge.Text.Thing.Prop.move_speed_factor.Display"), -0.02f, 0.02f, false, false, T("RimTalkGenKnowledge.Text.Thing.Prop.move_speed_factor.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.move_speed_factor.High"), 0.25f);
            EnsureProperty(config, "weapon_damage", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_damage.Display"), 10f, 20f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_damage.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_damage.High"));
            EnsureProperty(config, "weapon_range", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_range.Display"), 0f, 25f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_range.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_range.High"), 2f);
            EnsureProperty(config, "weapon_accuracy_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.Display"), 60f, 80f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.High"));
            EnsureProperty(config, "weapon_accuracy_short_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_short_pct.Display"), 60f, 90f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.High"));
            EnsureProperty(config, "weapon_accuracy_medium_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_medium_pct.Display"), 50f, 80f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.High"));
            EnsureProperty(config, "weapon_accuracy_long_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_long_pct.Display"), 40f, 70f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_accuracy_pct.High"));
            EnsureProperty(config, "weapon_burst_count", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_count.Display"), 1f, 3f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_count.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_count.High"));
            EnsureProperty(config, "weapon_burst_interval_s", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_interval_s.Display"), 0.5f, 1.5f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.interval.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.interval.High"));
            EnsureProperty(config, "weapon_warmup_s", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_warmup_s.Display"), 1f, 3f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_warmup_s.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_warmup_s.High"));
            EnsureProperty(config, "weapon_cooldown_s", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_cooldown_s.Display"), 1f, 3f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_cooldown_s.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_cooldown_s.High"));
            EnsureProperty(config, "weapon_suppression", T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_suppression.Display"), 0f, 1f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_suppression.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_suppression.High"));
            EnsureProperty(config, "turret_cooldown", T("RimTalkGenKnowledge.Text.Thing.Prop.turret_cooldown.Display"), 1f, 4f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.turret_cooldown.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.turret_cooldown.High"));
            EnsureProperty(config, "turret_warmup", T("RimTalkGenKnowledge.Text.Thing.Prop.turret_warmup.Display"), 1f, 4f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.turret_warmup.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.turret_warmup.High"));
            EnsureProperty(config, "turret_burst_count", T("RimTalkGenKnowledge.Text.Thing.Prop.turret_burst_count.Display"), 1f, 4f, true, false, T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_count.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.weapon_burst_count.High"));
            EnsureProperty(config, "armor_penetration_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.armor_penetration_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.armor_penetration_pct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.armor_penetration_pct.High"));
            EnsureProperty(config, "sharp_damage_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.sharp_damage_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.damagePct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.damagePct.High"));
            EnsureProperty(config, "blunt_damage_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.blunt_damage_pct.Display"), 50f, 100f, true, true, T("RimTalkGenKnowledge.Text.Thing.Prop.damagePct.Low"), T("RimTalkGenKnowledge.Text.Thing.Prop.damagePct.High"));
            EnsureProperty(config, "stuff_max_hit_points_pct", T("RimTalkGenKnowledge.Text.Thing.Prop.stuff_max_hit_points_pct.Display"), 50f, 150f, true, true, T("RimTalkGenKnowledge.Text.Thing.Stage.NegLight"), T("RimTalkGenKnowledge.Text.Thing.Stage.PosLight"), 1f, false);
        }

        private static void EnsureCategoryRule(ThingProcessDefConfig config, string key, string propertyKeys, int maxLines)
        {
            if (!config.CategoryRules.TryGetValue(key, out ThingCategoryRuleConfig rule) || rule == null)
            {
                config.CategoryRules[key] = new ThingCategoryRuleConfig { Enabled = true, PropertyKeys = propertyKeys, MaxLines = maxLines };
            }
        }

        private static void EnsureProperty(ThingProcessDefConfig config, string key, string displayName, float rangeMin, float rangeMax, bool nonNegativeOnly, bool isPercent, string lowLabel, string highLabel, float scale = 1f, bool enabledByDefault = true)
        {
            if (!config.PropertyDeviationConfigs.TryGetValue(key, out ThingPropertyDeviationConfig property) || property == null)
            {
                config.PropertyDeviationConfigs[key] = new ThingPropertyDeviationConfig
                {
                    Enabled = enabledByDefault,
                    DisplayName = displayName,
                    RangeMin = rangeMin,
                    RangeMax = rangeMax,
                    Scale = scale,
                    NonNegativeOnly = nonNegativeOnly,
                    IsPercent = isPercent,
                    StageTextNegStrong = T("RimTalkGenKnowledge.Text.Thing.Stage.NegStrong"),
                    StageTextNegLight = T("RimTalkGenKnowledge.Text.Thing.Stage.NegLight"),
                    StageTextPosLight = T("RimTalkGenKnowledge.Text.Thing.Stage.PosLight"),
                    StageTextPosStrong = T("RimTalkGenKnowledge.Text.Thing.Stage.PosStrong")
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(property.StageTextNegStrong) ||
                    string.Equals(property.StageTextNegStrong, "{bias}{label}", StringComparison.Ordinal))
                {
                    property.StageTextNegStrong = T("RimTalkGenKnowledge.Text.Thing.Stage.NegStrong");
                }
                if (string.IsNullOrWhiteSpace(property.StageTextNegLight) ||
                    string.Equals(property.StageTextNegLight, "{bias}{label}", StringComparison.Ordinal))
                {
                    property.StageTextNegLight = T("RimTalkGenKnowledge.Text.Thing.Stage.NegLight");
                }
                if (string.IsNullOrWhiteSpace(property.StageTextPosLight) ||
                    string.Equals(property.StageTextPosLight, "{bias}{label}", StringComparison.Ordinal))
                {
                    property.StageTextPosLight = T("RimTalkGenKnowledge.Text.Thing.Stage.PosLight");
                }
                if (string.IsNullOrWhiteSpace(property.StageTextPosStrong) ||
                    string.Equals(property.StageTextPosStrong, "{bias}{label}", StringComparison.Ordinal))
                {
                    property.StageTextPosStrong = T("RimTalkGenKnowledge.Text.Thing.Stage.PosStrong");
                }
            }
        }

        private static string T(string key)
        {
            return key.Translate();
        }
    }
}



