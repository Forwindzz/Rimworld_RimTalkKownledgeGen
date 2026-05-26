using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public partial class ThingDefProcessor
    {
        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig ?? (ThingProcessDefConfig)CreateDefaultConfig();
            EnsureDefaults(typed);

            int categoryCount = typed.CategoryRules?.Count ?? 0;
            int propertyCount = typed.PropertyDeviationConfigs?.Count ?? 0;
            const float lineHeight = 24f;
            const float gap = 6f;
            float categoryBlockHeight = lineHeight * 4f + gap * 4f;
            float propertyBlockHeight = lineHeight * 13f + gap * 14f;

            return 1040f + categoryCount * (categoryBlockHeight + gap) + propertyCount * (propertyBlockHeight + gap);
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, ThingProcessDefConfig config)
        {
            EnsureDefaults(config);

            Rect filterIntermediateRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterIntermediateRect, "Filter intermediate build states", ref config.FilterIntermediateBuildStates);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "Intermediate tokens", config.IntermediateBuildStateTokens, v => config.IntermediateBuildStateTokens = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, "Max description length", config.MaxDescriptionLength, v => config.MaxDescriptionLength = v);
            y += gap;
            Rect filterShortDescRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterShortDescRect, "Filter when description is shorter than label", ref config.FilterDescriptionShorterThanLabel);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Min description length (x label)", config.DescriptionMinLabelLengthMultiplier, v => config.DescriptionMinLabelLengthMultiplier = Mathf.Max(0f, v));
            y += gap;
            Rect filterEggVariantRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterEggVariantRect, "Filter fertilized/unfertilized egg variants", ref config.FilterFertilizedEggVariants);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "Egg variant tokens", config.FertilizedEggVariantTokens, v => config.FertilizedEggVariantTokens = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, "Max semantic lines (global)", config.MaxSemanticLinesGlobal, v => config.MaxSemanticLinesGlobal = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, "Special value TopN", config.SpecialValueTopN, v => config.SpecialValueTopN = v);
            y += gap;
            Rect fallbackRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(fallbackRect, "Enable fallback attribute output", ref config.EnableFallbackAttributeOutput);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, "Fallback max lines", config.FallbackAttributeMaxLines, v => config.FallbackAttributeMaxLines = v);
            y += gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "Fallback exclude keys", config.FallbackAttributeExcludeKeys, v => config.FallbackAttributeExcludeKeys = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight marketValue log10", config.ImportanceWeightMarketValueLog10, v => config.ImportanceWeightMarketValueLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight mass log10", config.ImportanceWeightMassLog10, v => config.ImportanceWeightMassLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight hitPoints log10", config.ImportanceWeightHitPointsLog10, v => config.ImportanceWeightHitPointsLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight stackLimit==1", config.ImportanceWeightStackLimitIsOne, v => config.ImportanceWeightStackLimitIsOne = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight special value score", config.ImportanceWeightSpecialValueScore, v => config.ImportanceWeightSpecialValueScore = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight nutrition", config.ImportanceWeightNutrition, v => config.ImportanceWeightNutrition = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Weight description/label length ratio", config.ImportanceWeightDescriptionLengthRatio, v => config.ImportanceWeightDescriptionLengthRatio = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "Craftable importance multiplier", config.ImportanceMultiplierCraftable, v => config.ImportanceMultiplierCraftable = Mathf.Max(0f, v));
            y += gap;
            Rect categoryTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(categoryTextRect, "Enable category extra text", ref config.EnableCategoryExtraText);
            y += lineHeight + gap;
            Rect priceTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(priceTextRect, "Enable market value tendency text", ref config.EnablePriceFeelingText);
            y += lineHeight + gap;
            Rect hpTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(hpTextRect, "Enable HP tendency text", ref config.EnableHitPointsFeelingText);
            y += lineHeight + gap;
            Rect debugDeviationRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(debugDeviationRect, "Debug: always show deviation details", ref config.DebugForceShowDeviation);
            y += lineHeight + gap;
            Rect infoRect = new Rect(x, y, width, lineHeight * 2f);
            Widgets.Label(infoRect, "Per-property deviation configs are persisted in settings and can be tuned later.");
            y += lineHeight * 2f + gap;

            Rect catHeader = new Rect(x, y, width, lineHeight);
            Widgets.Label(catHeader, "Category Rules");
            y += lineHeight + gap;

            foreach (string key in config.CategoryRules.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList())
            {
                ThingCategoryRuleConfig rule = config.CategoryRules[key];
                if (rule == null)
                {
                    rule = new ThingCategoryRuleConfig();
                    config.CategoryRules[key] = rule;
                }

                Rect categoryBox = new Rect(x, y, width, lineHeight * 4f + gap * 4f);
                Widgets.DrawMenuSection(categoryBox);
                float cy = categoryBox.y + 4f;
                cy = DrawCheckboxRow(x + 6f, cy, width - 12f, lineHeight, $"{key} Enabled", rule.Enabled, v => rule.Enabled = v) + gap;
                cy = ProcessDefUiUtility.DrawIntRow(x + 6f, cy, width - 12f, lineHeight, "Max lines", rule.MaxLines, v => rule.MaxLines = v) + gap;
                cy = ProcessDefUiUtility.DrawTextRow(x + 6f, cy, width - 12f, lineHeight, "Property keys (comma)", rule.PropertyKeys, v => rule.PropertyKeys = v) + gap;
                y += categoryBox.height + gap;
            }

            Rect propHeader = new Rect(x, y, width, lineHeight);
            Widgets.Label(propHeader, "Property Deviation Configs");
            y += lineHeight + gap;

            foreach (string key in config.PropertyDeviationConfigs.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList())
            {
                ThingPropertyDeviationConfig p = config.PropertyDeviationConfigs[key];
                if (p == null)
                {
                    p = new ThingPropertyDeviationConfig();
                    config.PropertyDeviationConfigs[key] = p;
                }

                // title + 11 editable rows + spacing
                float boxHeight = lineHeight * 12f + gap * 13f;
                Rect box = new Rect(x, y, width, boxHeight);
                Widgets.DrawMenuSection(box);
                float py = box.y + 4f;

                Rect title = new Rect(x + 6f, py, width - 12f, lineHeight);
                Widgets.Label(title, key);
                py += lineHeight + gap;

                py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, "Enabled", p.Enabled, v => p.Enabled = v) + gap;
                py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, "Display name", p.DisplayName, v => p.DisplayName = v) + gap;
                py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, "Range min", p.RangeMin, v => p.RangeMin = v) + gap;
                py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, "Range max", p.RangeMax, v => p.RangeMax = v) + gap;
                py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, "Scale", p.Scale, v => p.Scale = v) + gap;

                py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, "Non negative only", p.NonNegativeOnly, v => p.NonNegativeOnly = v) + gap;
                py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, "Is percent", p.IsPercent, v => p.IsPercent = v) + gap;

                py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, "Bias <-100%", p.StageTextNegStrong, v => p.StageTextNegStrong = v) + gap;
                py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, "Bias -100%~0%", p.StageTextNegLight, v => p.StageTextNegLight = v) + gap;
                py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, "Bias 0~100%", p.StageTextPosLight, v => p.StageTextPosLight = v) + gap;
                py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, "Bias >100%", p.StageTextPosStrong, v => p.StageTextPosStrong = v) + gap;

                y += box.height + gap;
            }

            return y;
        }

        private static float DrawCheckboxRow(float x, float y, float width, float lineHeight, string label, bool value, Action<bool> setter)
        {
            bool next = value;
            Widgets.CheckboxLabeled(new Rect(x, y, width, lineHeight), label, ref next);
            if (next != value)
            {
                setter(next);
            }
            return y + lineHeight;
        }
    }
}

