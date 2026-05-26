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
        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig ?? (ThingProcessDefConfig)CreateDefaultConfig();
            bool showNumericValues = context?.ShowNumericValues ?? false;
            EnsureDefaults(typed);
            if (!typed.Enabled)
            {
                yield break;
            }

            HashSet<string> intermediateTokens = BuildFilter(typed.IntermediateBuildStateTokens);
            HashSet<string> eggVariantTokens = BuildFilter(typed.FertilizedEggVariantTokens);

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

                if (!ProcessDefUtility.ShouldIncludeDef(def, typed.IncludeModDefs))
                {
                    continue;
                }

                string category = def.category.ToString();

                string label = ProcessDefUtility.TrimOrNull(def.label);
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                string description = ProcessDefUtility.TrimOrNull(def.description) ?? string.Empty;

                if (typed.MaxDescriptionLength > 0 && description.Length > typed.MaxDescriptionLength)
                {
                    description = description.Substring(0, typed.MaxDescriptionLength);
                }

                if (typed.FilterFertilizedEggVariants && IsFertilizedEggVariant(label, def.defName, description, eggVariantTokens))
                {
                    continue;
                }

                string kind = ResolveKind(def);
                if (typed.FilterIntermediateBuildStates && string.Equals(kind, KindBuilding, StringComparison.OrdinalIgnoreCase) && IsIntermediateState(def, label, description, intermediateTokens))
                {
                    continue;
                }

                if (!typed.CategoryRules.TryGetValue(kind, out ThingCategoryRuleConfig categoryRule) || categoryRule == null || !categoryRule.Enabled)
                {
                    continue;
                }

                float marketValue = def.BaseMarketValue;
                float mass = def.BaseMass;
                int stackLimit = def.stackLimit;
                float maxHitPoints = ResolveMaxHitPoints(def);
                bool hasTechLevel = def.techLevel != TechLevel.Undefined;
                string techLevel = LocalizeTechLevel(def.techLevel.ToString());
                string thingCategories = JoinThingCategoryLabels(def);
                string tradeTags = JoinStrings(def.tradeTags, true);
                string weaponTags = JoinStrings(def.weaponTags, true);
                string modSource = ResolveModSource(def);
                string categoryText = BuildCategoryText(LocalizeThingCategory(category), thingCategories, tradeTags, weaponTags);

                List<PropertyObservation> observations = BuildObservations(def, typed, typed.DebugForceShowDeviation, showNumericValues);
                int globalLimit = Math.Max(1, typed.MaxSemanticLinesGlobal);
                int lineLimit = globalLimit;
                List<PropertyObservation> selected = observations.OrderByDescending(o => o.StrengthD).Take(lineLimit).ToList();
                float specialValueScore = observations.Sum(o => o.StrengthD);
                bool showMarketLine = typed.DebugForceShowDeviation || IsObviousDeviation(selected, "market_value");
                bool showHpLine = typed.DebugForceShowDeviation || IsObviousDeviation(selected, "max_hit_points");

                List<SemanticLineEntry> semanticLines = BuildDefSemanticLines(def, typed, typed.DebugForceShowDeviation);
                string categoryExtraText = string.Empty;
                if (typed.EnableCategoryExtraText)
                {
                    HashSet<string> selectedKeys = new HashSet<string>(
                        selected.Where(o => !string.IsNullOrWhiteSpace(o.PropertyKey)).Select(o => o.PropertyKey),
                        StringComparer.OrdinalIgnoreCase);

                    // Avoid repeating the same concept in both dedicated line and deviation list.
                    if (showMarketLine)
                    {
                        selectedKeys.Add("market_value");
                    }
                    if (showHpLine)
                    {
                        selectedKeys.Add("max_hit_points");
                    }

                    var combined = new List<string>();
                    combined.AddRange(
                        selected
                            .Where(o => !(showMarketLine && string.Equals(o.PropertyKey, "market_value", StringComparison.OrdinalIgnoreCase)))
                            .Where(o => !(showHpLine && string.Equals(o.PropertyKey, "max_hit_points", StringComparison.OrdinalIgnoreCase)))
                            .Select(o => o.DisplayLine)
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                    combined.AddRange(
                        semanticLines
                            .Where(s => !string.IsNullOrWhiteSpace(s?.Text))
                            .Where(s => showNumericValues || string.IsNullOrWhiteSpace(s.PropertyKey))
                            .Where(s => showNumericValues || !selectedKeys.Contains(s.PropertyKey))
                            .Where(s => showNumericValues || !ContainsDigit(s.Text))
                            .Select(s => s.Text));
                    categoryExtraText = string.Join("；", combined.Take(lineLimit).ToArray());
                }

                string marketValueText = typed.EnablePriceFeelingText
                    ? BuildValueWithTendency(marketValue, selected, "market_value", showNumericValues)
                    : marketValue.ToString("0.##", CultureInfo.InvariantCulture);
                string hpText = typed.EnableHitPointsFeelingText
                    ? BuildValueWithTendency(maxHitPoints, selected, "max_hit_points", showNumericValues)
                    : maxHitPoints.ToString("0.##", CultureInfo.InvariantCulture);
                string techLevelLine = hasTechLevel ? Tr("RimTalkGenKnowledge.Text.Thing.Line.TechLevel").Formatted(techLevel).ToString() : string.Empty;
                string modSourceLine = string.Equals(modSource, "Core", StringComparison.OrdinalIgnoreCase) ? string.Empty : Tr("RimTalkGenKnowledge.Text.Thing.Line.ModSourceConcept").Formatted(modSource).ToString();
                string marketValueLine = showMarketLine ? Tr("RimTalkGenKnowledge.Text.Thing.Line.MarketValue").Formatted(marketValueText).ToString() : string.Empty;
                string hpLine = showHpLine ? Tr("RimTalkGenKnowledge.Text.Thing.Line.HitPoints").Formatted(hpText).ToString() : string.Empty;
                if (typed.FilterDescriptionShorterThanLabel)
                {
                    int minLength = Mathf.CeilToInt(label.Length * Mathf.Max(0f, typed.DescriptionMinLabelLengthMultiplier));
                    if (string.IsNullOrWhiteSpace(description) || description.Length < minLength)
                    {
                        continue;
                    }
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["labelDelimiter"] = Tr("RimTalkGenKnowledge.Text.Thing.Line.LabelDelimiter"),
                    ["description"] = description,
                    ["category"] = category,
                    ["categoryPrefix"] = Tr("RimTalkGenKnowledge.Text.Thing.Line.CategoryPrefix"),
                    ["marketValue"] = marketValue.ToString("0.##", CultureInfo.InvariantCulture),
                    ["mass"] = mass.ToString("0.##", CultureInfo.InvariantCulture),
                    ["stackLimit"] = stackLimit.ToString(CultureInfo.InvariantCulture),
                    ["techLevel"] = techLevel,
                    ["defName"] = def.defName,
                    ["categoryText"] = categoryText,
                    ["techLevelText"] = techLevel,
                    ["modSource"] = modSource,
                    ["modSourceLine"] = modSourceLine,
                    ["marketValueText"] = marketValueText,
                    ["hpText"] = hpText,
                    ["techLevelLine"] = techLevelLine,
                    ["marketValueLine"] = marketValueLine,
                    ["hpLine"] = hpLine,
                    ["categoryExtraText"] = categoryExtraText,
                    ["thingCategories"] = thingCategories,
                    ["tradeTags"] = tradeTags,
                    ["weaponTags"] = weaponTags,
                    ["maxHitPoints"] = maxHitPoints.ToString("0.##", CultureInfo.InvariantCulture)
                });

                string tag = RenderTag(typed);
                string content = NormalizeMultilineContent(RenderContent(typed));
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float marketMetric = ProcessDefUtility.SafeLog10(marketValue);
                float massMetric = ProcessDefUtility.SafeLog10(mass);
                float hpMetric = ProcessDefUtility.SafeLog10(maxHitPoints);
                float stackMetric = stackLimit == 1 ? 1f : 0f;
                float nutritionMetric = Mathf.Max(0f, ResolveNutrition(def));
                float descriptionLengthRatio = label.Length > 0 ? (float)description.Length / label.Length : 0f;
                float raw = typed.BaseImportance
                    + marketMetric * typed.ImportanceWeightMarketValueLog10
                    + Math.Abs(massMetric) * typed.ImportanceWeightMassLog10
                    + hpMetric * typed.ImportanceWeightHitPointsLog10
                    + Math.Abs(stackMetric) * typed.ImportanceWeightStackLimitIsOne
                    + specialValueScore * typed.ImportanceWeightSpecialValueScore
                    + nutritionMetric * typed.ImportanceWeightNutrition
                    + descriptionLengthRatio * typed.ImportanceWeightDescriptionLengthRatio;
                if (IsCraftableThingDef(def))
                {
                    raw *= Mathf.Max(0f, typed.ImportanceMultiplierCraftable);
                }

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "thing:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static List<PropertyObservation> BuildObservations(ThingDef def, ThingProcessDefConfig config, bool forceShowDebug, bool showNumericValues)
        {
            var observations = new List<PropertyObservation>();
            foreach (var pair in config.PropertyDeviationConfigs)
            {
                string key = pair.Key;
                ThingPropertyDeviationConfig propertyConfig = pair.Value;
                if (propertyConfig == null || !propertyConfig.Enabled)
                {
                    continue;
                }

                if (!TryResolvePropertyValue(def, key, propertyConfig, out float value, out string valueText, out bool nonNegativeOnly, out bool baseValueIsZero))
                {
                    continue;
                }
                if (baseValueIsZero)
                {
                    continue;
                }

                float c = ComputeSignedDeviation(value, propertyConfig, nonNegativeOnly);
                if (Mathf.Approximately(c, 0f) && !forceShowDebug)
                {
                    continue;
                }

                float cScaled = c * propertyConfig.Scale;
                float d = Mathf.Abs(Mathf.Log(Mathf.Abs(cScaled) + 1f));
                string tendencyText;
                if (Mathf.Approximately(cScaled, 0f))
                {
                    tendencyText = Tr("RimTalkGenKnowledge.Text.Thing.Tendency.WithinRange");
                }
                else
                {
                    string stageText = ResolveStageText(propertyConfig, cScaled);
                    tendencyText = string.IsNullOrWhiteSpace(stageText) ? Tr("RimTalkGenKnowledge.Text.Thing.Tendency.Default") : stageText;
                }
                string displayName = string.IsNullOrWhiteSpace(propertyConfig.DisplayName) ? key : propertyConfig.DisplayName;
                string displayLine = showNumericValues
                    ? (displayName + "=" + valueText + " (" + tendencyText + ")")
                    : (displayName + tendencyText);
                if (forceShowDebug)
                {
                    displayLine += $" [c'={cScaled:0.###}, d={d:0.###}]";
                }

                observations.Add(new PropertyObservation
                {
                    PropertyKey = key,
                    SignedC = cScaled,
                    StrengthD = d,
                    TendencyText = tendencyText,
                    DisplayLine = displayLine
                });
            }

            return observations;
        }

        private static float ComputeSignedDeviation(float value, ThingPropertyDeviationConfig cfg, bool nonNegativeBySource)
        {
            float a = cfg.RangeMin;
            float b = cfg.RangeMax;
            if (Mathf.Approximately(a, b))
            {
                return 0f;
            }

            float c = 0f;
            if (value < a)
            {
                c = -((a - value) / (b - a));
            }
            else if (value > b)
            {
                c = (value - b) / (b - a);
            }

            bool treatZeroAsExtremeLow = cfg.NonNegativeOnly || nonNegativeBySource;
            if (treatZeroAsExtremeLow && Mathf.Approximately(value, 0f))
            {
                c = -2f;
            }

            return c;
        }

        private static string ResolveStageText(ThingPropertyDeviationConfig cfg, float cScaled)
        {
            if (cScaled < -1f)
            {
                return string.IsNullOrWhiteSpace(cfg.StageTextNegStrong) ? Tr("RimTalkGenKnowledge.Text.Thing.Stage.NegStrong") : cfg.StageTextNegStrong;
            }

            if (cScaled <= 0f)
            {
                return string.IsNullOrWhiteSpace(cfg.StageTextNegLight) ? Tr("RimTalkGenKnowledge.Text.Thing.Stage.NegLight") : cfg.StageTextNegLight;
            }

            if (cScaled <= 1f)
            {
                return string.IsNullOrWhiteSpace(cfg.StageTextPosLight) ? Tr("RimTalkGenKnowledge.Text.Thing.Stage.PosLight") : cfg.StageTextPosLight;
            }

            return string.IsNullOrWhiteSpace(cfg.StageTextPosStrong) ? Tr("RimTalkGenKnowledge.Text.Thing.Stage.PosStrong") : cfg.StageTextPosStrong;
        }

        private static string ResolveKind(ThingDef def)
        {
            if (ProcessDefUtility.GetBoolMemberOrDefault(def, "IsWeapon", false))
            {
                return KindWeapon;
            }
            if (ProcessDefUtility.GetBoolMemberOrDefault(def, "IsApparel", false))
            {
                return KindApparel;
            }
            if (ProcessDefUtility.GetBoolMemberOrDefault(def, "IsMedicine", false))
            {
                return KindMedicine;
            }
            if (ProcessDefUtility.GetBoolMemberOrDefault(def, "IsNutritionGivingIngestible", false) || ProcessDefUtility.TryGetMemberValue(def, "ingestible", out object ingestible) && ingestible != null)
            {
                return KindFood;
            }
            if (def.category == ThingCategory.Building || def.building != null)
            {
                return KindBuilding;
            }
            return KindItem;
        }

        private static bool IsIntermediateState(ThingDef def, string label, string description, HashSet<string> tokens)
        {
            if (def.defName.StartsWith("Blueprint_", StringComparison.OrdinalIgnoreCase) ||
                def.defName.StartsWith("Frame_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (tokens == null || tokens.Count == 0)
            {
                return false;
            }

            string text = ((label ?? string.Empty) + " " + (description ?? string.Empty) + " " + (def.defName ?? string.Empty)).ToLowerInvariant();
            foreach (string token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token) && text.Contains(token.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        private static string BuildCategoryText(string category, string thingCategories, string tradeTags, string weaponTags)
        {
            var tokens = new List<string>();
            tokens.AddRange(ParseCategoryTokens(category));
            tokens.AddRange(ParseCategoryTokens(thingCategories));
            tokens.AddRange(ParseCategoryTokens(tradeTags));
            tokens.AddRange(ParseCategoryTokens(weaponTags));

            string[] unique = tokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (unique.Length == 0)
            {
                return string.Empty;
            }

            string separator = Tr("RimTalkGenKnowledge.Text.Thing.Separator.CategoryList");
            string joined = string.Join(separator, unique);
            return Tr("RimTalkGenKnowledge.Text.Thing.Line.CategoryMerged").Formatted(joined).ToString();
        }

        private static IEnumerable<string> ParseCategoryTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            string[] parts = text.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i]?.Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (IsHiddenMiscToken(token))
                {
                    continue;
                }

                yield return token;
            }
        }

        private static bool IsHiddenMiscToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return string.Equals(token, "杂项", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "misc", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildValueWithTendency(float value, List<PropertyObservation> observations, string propertyKey, bool showNumericValues)
        {
            PropertyObservation obs = observations.FirstOrDefault(o => string.Equals(o.PropertyKey, propertyKey, StringComparison.OrdinalIgnoreCase));
            if (obs == null)
            {
                return showNumericValues ? value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
            }

            string tendency = string.IsNullOrWhiteSpace(obs.TendencyText) ? obs.DisplayLine : obs.TendencyText;
            return showNumericValues
                ? value.ToString("0.##", CultureInfo.InvariantCulture) + " (" + tendency + ")"
                : tendency;
        }

        private static bool IsObviousDeviation(List<PropertyObservation> observations, string propertyKey)
        {
            if (observations == null || string.IsNullOrWhiteSpace(propertyKey))
            {
                return false;
            }

            PropertyObservation obs = observations.FirstOrDefault(o => string.Equals(o.PropertyKey, propertyKey, StringComparison.OrdinalIgnoreCase));
            if (obs == null)
            {
                return false;
            }

            return Mathf.Abs(obs.SignedC) > 1f;
        }

        private static string NormalizeMultilineContent(string content)
        {
            return TextNormalizeUtility.NormalizeMultiline(content, "\n");
        }

        private static bool ContainsDigit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<SemanticLineEntry> BuildDefSemanticLines(ThingDef def, ThingProcessDefConfig config, bool debug)
        {
            var lines = new List<SemanticLineEntry>();
            if (def == null)
            {
                return lines;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "terrainAffordanceNeeded", out object terrainAffordance) && terrainAffordance != null)
            {
                string terrainLine = BuildTerrainAffordanceLine(terrainAffordance);
                if (!string.IsNullOrWhiteSpace(terrainLine))
                {
                    AddSemanticLine(lines, config, null, terrainLine);
                }
            }

            float constructionSkill = Mathf.Max(0f, ProcessDefUtility.GetFloatMemberOrDefault(def, "constructionSkillPrerequisite", 0f));
            if (!Mathf.Approximately(constructionSkill, 0f))
            {
                AddSemanticLine(lines, config, "construction_skill_prerequisite", Tr("RimTalkGenKnowledge.Text.Thing.Line.ConstructionSkill").Formatted(constructionSkill.ToString("0.#", CultureInfo.InvariantCulture)));
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "costList", out object costListObj) && costListObj is System.Collections.IList costList && costList.Count > 0)
            {
                string costText = BuildCostListText(costList, 4);
                if (!string.IsNullOrWhiteSpace(costText))
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.BuildCost").Formatted(costText));
                }
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "building", out object building) && building != null)
            {
                bool isSittable = ProcessDefUtility.GetBoolMemberOrDefault(building, "isSittable", false);
                if (isSittable)
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.Sittable"));
                }

                bool paintable = ProcessDefUtility.GetBoolMemberOrDefault(building, "paintable", false);
                if (paintable)
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.Paintable"));
                }
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "apparel", out object apparel) && apparel != null)
            {
                string layers = JoinDefNamesOrLabels(apparel, "layers");
                if (!string.IsNullOrWhiteSpace(layers))
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.ApparelLayers").Formatted(layers));
                }

                string bodyParts = JoinDefNamesOrLabels(apparel, "bodyPartGroups");
                if (!string.IsNullOrWhiteSpace(bodyParts))
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.ApparelBodyParts").Formatted(bodyParts));
                }
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "plant", out object plant) && plant != null)
            {
                float growDays = ProcessDefUtility.GetFloatMemberOrDefault(plant, "growDays", 0f);
                if (!Mathf.Approximately(growDays, 0f))
                {
                    AddSemanticLine(lines, config, "grow_days", Tr("RimTalkGenKnowledge.Text.Thing.Line.GrowDays").Formatted(growDays.ToString("0.##", CultureInfo.InvariantCulture), DayUnit()));
                }

                float fertilityMin = ProcessDefUtility.GetFloatMemberOrDefault(plant, "fertilityMin", 0f);
                if (!Mathf.Approximately(fertilityMin, 0f))
                {
                    float pct = fertilityMin <= 2f ? fertilityMin * 100f : fertilityMin;
                    AddSemanticLine(lines, config, "fertility_min_pct", Tr("RimTalkGenKnowledge.Text.Thing.Line.FertilityMin").Formatted(pct.ToString("0.#", CultureInfo.InvariantCulture)));
                }
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "race", out object race) && race != null)
            {
                float life = ProcessDefUtility.GetFloatMemberOrDefault(race, "lifeExpectancy", 0f);
                if (!Mathf.Approximately(life, 0f))
                {
                    AddSemanticLine(lines, config, "life_expectancy", Tr("RimTalkGenKnowledge.Text.Thing.Line.LifeExpectancy").Formatted(life.ToString("0.##", CultureInfo.InvariantCulture)));
                }

                float gestation = ProcessDefUtility.GetFloatMemberOrDefault(race, "gestationPeriodDays", 0f);
                if (gestation < 0f)
                {
                    AddSemanticLine(lines, config, null, Tr("RimTalkGenKnowledge.Text.Thing.Line.CannotGestate"));
                }
                else if (!Mathf.Approximately(gestation, 0f))
                {
                    AddSemanticLine(lines, config, "gestation_days", Tr("RimTalkGenKnowledge.Text.Thing.Line.GestationDays").Formatted(gestation.ToString("0.##", CultureInfo.InvariantCulture), DayUnit()));
                }
            }

            AppendCompSemantic(lines, config, def, "milkable", "milkAmount", "milk_amount", Tr("RimTalkGenKnowledge.Text.Thing.Line.MilkAmountPrefix"), string.Empty);
            AppendCompSemantic(lines, config, def, "milkable", "milkIntervalDays", "milk_interval_days", Tr("RimTalkGenKnowledge.Text.Thing.Line.MilkIntervalPrefix"), DayUnit());
            AppendCompSemantic(lines, config, def, "shearable", "woolAmount", "wool_amount", Tr("RimTalkGenKnowledge.Text.Thing.Line.WoolAmountPrefix"), string.Empty);
            AppendCompSemantic(lines, config, def, "shearable", "shearIntervalDays", "shear_interval_days", Tr("RimTalkGenKnowledge.Text.Thing.Line.ShearIntervalPrefix"), DayUnit());
            AppendCompSemantic(lines, config, def, "egglayer", "eggLayIntervalDays", "egg_interval_days", Tr("RimTalkGenKnowledge.Text.Thing.Line.EggIntervalPrefix"), DayUnit());

            if (debug)
            {
                AddSemanticLine(lines, config, null, "kind=" + ResolveKind(def));
            }

            return lines;
        }

        private static bool IsFertilizedEggVariant(string label, string defName, string description, HashSet<string> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return false;
            }

            string text = ((label ?? string.Empty) + " " + (defName ?? string.Empty) + " " + (description ?? string.Empty)).ToLowerInvariant();
            foreach (string token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (text.Contains(token.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCraftableThingDef(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "recipeMaker", out object recipeMaker) && recipeMaker != null)
            {
                return true;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "costList", out object costListObj) &&
                costListObj is System.Collections.IList costList &&
                costList.Count > 0)
            {
                return true;
            }

            float costStuffCount = ProcessDefUtility.GetFloatMemberOrDefault(def, "costStuffCount", 0f);
            return costStuffCount > 0f;
        }

        private static string BuildTerrainAffordanceLine(object terrainAffordance)
        {
            if (terrainAffordance == null)
            {
                return string.Empty;
            }

            string label = ProcessDefUtility.GetStringMemberOrDefault(terrainAffordance, "label", string.Empty);
            string defName = ProcessDefUtility.GetStringMemberOrDefault(terrainAffordance, "defName", string.Empty);
            string text = ((label ?? string.Empty) + " " + (defName ?? string.Empty)).ToLowerInvariant();

            // Light: do not output.
            if (text.Contains("light"))
            {
                return string.Empty;
            }

            if (text.Contains("heavy"))
            {
                return Tr("RimTalkGenKnowledge.Text.Thing.Line.TerrainAffordance.Heavy");
            }

            if (text.Contains("medium"))
            {
                return Tr("RimTalkGenKnowledge.Text.Thing.Line.TerrainAffordance.Medium");
            }

            return string.Empty;
        }

        private static bool IsPropertyEnabled(ThingProcessDefConfig config, string propertyKey)
        {
            if (string.IsNullOrWhiteSpace(propertyKey) || config?.PropertyDeviationConfigs == null)
            {
                return true;
            }

            if (!config.PropertyDeviationConfigs.TryGetValue(propertyKey, out ThingPropertyDeviationConfig property) || property == null)
            {
                return true;
            }

            return property.Enabled;
        }

        private static void AddSemanticLine(List<SemanticLineEntry> lines, ThingProcessDefConfig config, string propertyKey, string text)
        {
            if (lines == null || string.IsNullOrWhiteSpace(text) || !IsPropertyEnabled(config, propertyKey))
            {
                return;
            }

            lines.Add(new SemanticLineEntry
            {
                PropertyKey = propertyKey,
                Text = text
            });
        }

        private static void AppendCompSemantic(List<SemanticLineEntry> lines, ThingProcessDefConfig config, ThingDef def, string compToken, string memberName, string propertyKey, string prefix, string suffix)
        {
            if (lines == null || def?.comps == null)
            {
                return;
            }

            string token = (compToken ?? string.Empty).ToLowerInvariant();
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

                float value = ProcessDefUtility.GetFloatMemberOrDefault(comp, memberName, 0f);
                if (Mathf.Approximately(value, 0f))
                {
                    continue;
                }

                AddSemanticLine(lines, config, propertyKey, prefix + value.ToString("0.##", CultureInfo.InvariantCulture) + suffix);
                return;
            }
        }

        private static string BuildCostListText(System.Collections.IList costList, int maxItems)
        {
            if (costList == null || costList.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            int limit = Math.Max(1, maxItems);
            for (int i = 0; i < costList.Count && parts.Count < limit; i++)
            {
                object entry = costList[i];
                if (entry == null)
                {
                    continue;
                }

                float count = ProcessDefUtility.GetFloatMemberOrDefault(entry, "count", 0f);
                if (!ProcessDefUtility.TryGetMemberValue(entry, "thingDef", out object thingDefObj) || thingDefObj == null)
                {
                    continue;
                }

                string label = ProcessDefUtility.TrimOrNull(ProcessDefUtility.GetStringMemberOrDefault(thingDefObj, "label", string.Empty))
                    ?? ProcessDefUtility.GetStringMemberOrDefault(thingDefObj, "defName", string.Empty);
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                parts.Add(count.ToString("0.#", CultureInfo.InvariantCulture) + "x " + label);
            }

            return string.Join(", ", parts.ToArray());
        }

        private static string JoinDefNamesOrLabels(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return string.Empty;
            }

            if (!ProcessDefUtility.TryGetMemberValue(target, memberName, out object listObj) || !(listObj is System.Collections.IList list))
            {
                return string.Empty;
            }

            var values = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                object entry = list[i];
                if (entry == null)
                {
                    continue;
                }

                string label = ProcessDefUtility.TrimOrNull(ProcessDefUtility.GetStringMemberOrDefault(entry, "label", string.Empty));
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = ProcessDefUtility.GetStringMemberOrDefault(entry, "defName", string.Empty);
                }

                if (!string.IsNullOrWhiteSpace(label))
                {
                    values.Add(label);
                }
            }

            return string.Join(",", values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray());
        }

        private static string JoinThingCategoryLabels(ThingDef def)
        {
            if (def?.thingCategories == null || def.thingCategories.Count == 0)
            {
                return string.Empty;
            }

            var values = new List<string>();
            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                ThingCategoryDef category = def.thingCategories[i];
                string label = ProcessDefUtility.TrimOrNull(category?.label) ?? category?.defName;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    values.Add(label);
                }
            }
            return string.Join(",", values.ToArray());
        }

        private static string JoinStrings(List<string> values, bool tryLocalize = false)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            IEnumerable<string> normalized = values.Where(v => !string.IsNullOrWhiteSpace(v));
            if (tryLocalize)
            {
                normalized = normalized.Select(LocalizeTagToken);
            }

            return string.Join(",", normalized.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToArray());
        }

        private static string LocalizeTagToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            string key = "RimTalkGenKnowledge.Text.Thing.Tag." + token;
            return ProcessDefUtility.TranslateKeyOrFallback(key, token);
        }

        private static string LocalizeThingCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return string.Empty;
            }

            string key = "RimTalkGenKnowledge.Text.Thing.Category." + category;
            return ProcessDefUtility.TranslateKeyOrFallback(key, category);
        }

        private static string LocalizeTechLevel(string techLevel)
        {
            return ProcessDefUtility.LocalizeTechLevel(techLevel);
        }

        private static string ResolveModSource(ThingDef def)
        {
            if (def?.modContentPack == null)
            {
                return "Core";
            }
            string name = ProcessDefUtility.GetStringMemberOrDefault(def.modContentPack, "Name", string.Empty);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            string packageId = ProcessDefUtility.ReadPackageId(def.modContentPack);
            return string.IsNullOrWhiteSpace(packageId) ? "Unknown" : packageId;
        }

        private static string Tr(string key)
        {
            return key.Translate();
        }

        private static string DayUnit()
        {
            return Tr("RimTalkGenKnowledge.Text.Unit.DaySuffix");
        }
    }
}

