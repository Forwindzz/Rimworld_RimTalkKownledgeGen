using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public partial class ThingDefProcessor
    {
        private static bool categoryRulesExpanded = true;
        private static bool propertyDeviationConfigsExpanded = false;

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig ?? (ThingProcessDefConfig)CreateDefaultConfig();
            EnsureDefaults(typed);

            int categoryCount = typed.CategoryRules?.Count ?? 0;
            int propertyCount = typed.PropertyDeviationConfigs?.Count ?? 0;
            const float lineHeight = 24f;
            const float gap = 6f;
            // Keep this strictly aligned with DrawConfig + DrawAdvancedConfig increments.
            const float commonHeight = 360f; // ProcessDefProcessorBase.DrawConfig fixed section
            float categoryBlockDrawHeight = lineHeight * 4f + gap * 4f;
            float propertyBlockDrawHeight = lineHeight * 12f + gap * 13f;

            // Advanced top rows before foldout headers:
            // 24 standard rows (line+gap) + one 2-line info row.
            float advancedBaseHeight = 24f * (lineHeight + gap) + (lineHeight * 2f + gap);

            // Two foldout headers (Category Rules + Property Deviation Configs).
            float foldoutHeadersHeight = 2f * (lineHeight + gap);

            float categoryExpandedHeight = categoryRulesExpanded
                ? categoryCount * (categoryBlockDrawHeight + gap)
                : 0f;

            float propertyExpandedHeight = propertyDeviationConfigsExpanded
                ? propertyCount * (propertyBlockDrawHeight + gap)
                : 0f;

            // Extra safe margin to avoid clipping at bottom due to contracted container rect.
            const float safetyMargin = 20f;

            return commonHeight
                + advancedBaseHeight
                + foldoutHeadersHeight
                + categoryExpandedHeight
                + propertyExpandedHeight
                + safetyMargin;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, ThingProcessDefConfig config)
        {
            EnsureDefaults(config);

            Rect filterIntermediateRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterIntermediateRect, T("RimTalkGenKnowledge.Settings.Thing.FilterIntermediateBuildStates"), ref config.FilterIntermediateBuildStates);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.IntermediateTokens"), config.IntermediateBuildStateTokens, v => config.IntermediateBuildStateTokens = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.MaxDescriptionLength"), config.MaxDescriptionLength, v => config.MaxDescriptionLength = v);
            y += gap;
            Rect filterShortDescRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterShortDescRect, T("RimTalkGenKnowledge.Settings.Thing.FilterShortDescription"), ref config.FilterDescriptionShorterThanLabel);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.MinDescriptionLengthMultiplier"), config.DescriptionMinLabelLengthMultiplier, v => config.DescriptionMinLabelLengthMultiplier = Mathf.Max(0f, v));
            y += gap;
            Rect filterEggVariantRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(filterEggVariantRect, T("RimTalkGenKnowledge.Settings.Thing.FilterEggVariants"), ref config.FilterFertilizedEggVariants);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.EggVariantTokens"), config.FertilizedEggVariantTokens, v => config.FertilizedEggVariantTokens = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.MaxSemanticLinesGlobal"), config.MaxSemanticLinesGlobal, v => config.MaxSemanticLinesGlobal = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.SpecialValueTopN"), config.SpecialValueTopN, v => config.SpecialValueTopN = v);
            y += gap;
            Rect fallbackRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(fallbackRect, T("RimTalkGenKnowledge.Settings.Thing.EnableFallbackAttributeOutput"), ref config.EnableFallbackAttributeOutput);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.FallbackMaxLines"), config.FallbackAttributeMaxLines, v => config.FallbackAttributeMaxLines = v);
            y += gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.FallbackExcludeKeys"), config.FallbackAttributeExcludeKeys, v => config.FallbackAttributeExcludeKeys = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightMarketValueLog10"), config.ImportanceWeightMarketValueLog10, v => config.ImportanceWeightMarketValueLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightMassLog10"), config.ImportanceWeightMassLog10, v => config.ImportanceWeightMassLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightHitPointsLog10"), config.ImportanceWeightHitPointsLog10, v => config.ImportanceWeightHitPointsLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightStackLimitIsOne"), config.ImportanceWeightStackLimitIsOne, v => config.ImportanceWeightStackLimitIsOne = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightSpecialValueScore"), config.ImportanceWeightSpecialValueScore, v => config.ImportanceWeightSpecialValueScore = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightNutrition"), config.ImportanceWeightNutrition, v => config.ImportanceWeightNutrition = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.WeightDescriptionLengthRatio"), config.ImportanceWeightDescriptionLengthRatio, v => config.ImportanceWeightDescriptionLengthRatio = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.CraftableImportanceMultiplier"), config.ImportanceMultiplierCraftable, v => config.ImportanceMultiplierCraftable = Mathf.Max(0f, v));
            y += gap;
            Rect categoryTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(categoryTextRect, T("RimTalkGenKnowledge.Settings.Thing.EnableCategoryExtraText"), ref config.EnableCategoryExtraText);
            y += lineHeight + gap;
            Rect priceTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(priceTextRect, T("RimTalkGenKnowledge.Settings.Thing.EnableMarketValueTendencyText"), ref config.EnablePriceFeelingText);
            y += lineHeight + gap;
            Rect hpTextRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(hpTextRect, T("RimTalkGenKnowledge.Settings.Thing.EnableHpTendencyText"), ref config.EnableHitPointsFeelingText);
            y += lineHeight + gap;
            Rect debugDeviationRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(debugDeviationRect, T("RimTalkGenKnowledge.Settings.Thing.DebugAlwaysShowDeviationDetails"), ref config.DebugForceShowDeviation);
            y += lineHeight + gap;
            Rect infoRect = new Rect(x, y, width, lineHeight * 2f);
            Widgets.Label(infoRect, T("RimTalkGenKnowledge.Settings.Thing.PerPropertyDeviationHint"));
            y += lineHeight * 2f + gap;

            categoryRulesExpanded = DrawFoldoutHeader(new Rect(x, y, width, lineHeight), T("RimTalkGenKnowledge.Settings.Thing.CategoryRules"), categoryRulesExpanded);
            y += lineHeight + gap;

            if (categoryRulesExpanded)
            {
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
                    cy = DrawCheckboxRow(x + 6f, cy, width - 12f, lineHeight, "RimTalkGenKnowledge.Settings.Thing.CategoryRuleEnabledFormat".Translate(key), rule.Enabled, v => rule.Enabled = v) + gap;
                    cy = ProcessDefUiUtility.DrawIntRow(x + 6f, cy, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.MaxLines"), rule.MaxLines, v => rule.MaxLines = v) + gap;
                    cy = ProcessDefUiUtility.DrawTextRow(x + 6f, cy, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.PropertyKeysComma"), rule.PropertyKeys, v => rule.PropertyKeys = v) + gap;
                    y += categoryBox.height + gap;
                }
            }

            propertyDeviationConfigsExpanded = DrawFoldoutHeader(new Rect(x, y, width, lineHeight), T("RimTalkGenKnowledge.Settings.Thing.PropertyDeviationConfigs"), propertyDeviationConfigsExpanded);
            y += lineHeight + gap;

            if (propertyDeviationConfigsExpanded)
            {
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

                    py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Enabled"), p.Enabled, v => p.Enabled = v) + gap;
                    py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.DisplayName"), p.DisplayName, v => p.DisplayName = v) + gap;
                    py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.RangeMin"), p.RangeMin, v => p.RangeMin = v) + gap;
                    py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.RangeMax"), p.RangeMax, v => p.RangeMax = v) + gap;
                    py = ProcessDefUiUtility.DrawFloatRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.Scale"), p.Scale, v => p.Scale = v) + gap;

                    py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.NonNegativeOnly"), p.NonNegativeOnly, v => p.NonNegativeOnly = v) + gap;
                    py = DrawCheckboxRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.IsPercent"), p.IsPercent, v => p.IsPercent = v) + gap;

                    py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.BiasLtNeg100"), p.StageTextNegStrong, v => p.StageTextNegStrong = v) + gap;
                    py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.BiasNeg100To0"), p.StageTextNegLight, v => p.StageTextNegLight = v) + gap;
                    py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.Bias0To100"), p.StageTextPosLight, v => p.StageTextPosLight = v) + gap;
                    py = ProcessDefUiUtility.DrawTextRow(x + 6f, py, width - 12f, lineHeight, T("RimTalkGenKnowledge.Settings.Thing.BiasGt100"), p.StageTextPosStrong, v => p.StageTextPosStrong = v) + gap;

                    y += box.height + gap;
                }
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

        private static bool DrawFoldoutHeader(Rect rect, string label, bool expanded)
        {
            string foldoutText = (expanded ? "▼ " : "▶ ") + label;
            if (Widgets.ButtonText(rect, foldoutText))
            {
                return !expanded;
            }

            return expanded;
        }
    }
}

