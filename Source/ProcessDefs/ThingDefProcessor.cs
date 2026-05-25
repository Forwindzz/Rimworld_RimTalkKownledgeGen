using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class ThingDefProcessor : ProcessDefProcessorBase<ThingProcessDefConfig>
    {
        public const string ProcessorId = "ThingDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Thing.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new ThingProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}} (value={{marketValue}}, mass={{mass}})",
                BaseImportance = 0.1f,
                ImportanceMin = 0f,
                ImportanceMax = 0.8f,
                IncludeCategories = "Weapon,Apparel,Medicine,Food,Building",
                ExcludeCategories = string.Empty,
                MaxDescriptionLength = 300,
                IncludeStatSummary = false,
                ImportanceWeightMarketValueLog10 = 0.05f,
                ImportanceWeightMassLog10 = 0.001f,
                ImportanceWeightStackLimitIsOne = 0.05f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            ThingProcessDefConfig defaults = (ThingProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludeCategories = defaults.IncludeCategories;
            typed.ExcludeCategories = defaults.ExcludeCategories;
            typed.MaxDescriptionLength = defaults.MaxDescriptionLength;
            typed.IncludeStatSummary = defaults.IncludeStatSummary;
            typed.ImportanceWeightMarketValueLog10 = defaults.ImportanceWeightMarketValueLog10;
            typed.ImportanceWeightMassLog10 = defaults.ImportanceWeightMassLog10;
            typed.ImportanceWeightStackLimitIsOne = defaults.ImportanceWeightStackLimitIsOne;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Thing label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Thing description" },
                new PlaceholderDescriptor { Key = "category", Token = "{{category}}", Description = "Thing category" },
                new PlaceholderDescriptor { Key = "marketValue", Token = "{{marketValue}}", Description = "Market value" },
                new PlaceholderDescriptor { Key = "mass", Token = "{{mass}}", Description = "Mass" },
                new PlaceholderDescriptor { Key = "stackLimit", Token = "{{stackLimit}}", Description = "Stack limit" },
                new PlaceholderDescriptor { Key = "techLevel", Token = "{{techLevel}}", Description = "Tech level" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 620f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, ThingProcessDefConfig config)
        {
            Rect includeStatRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(includeStatRect, "RimTalkGenKnowledge.Settings.Thing.IncludeStatSummary".Translate(), ref config.IncludeStatSummary);
            y += lineHeight + gap;

            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.IncludeCategories".Translate(), config.IncludeCategories, v => config.IncludeCategories = v);
            y += gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.ExcludeCategories".Translate(), config.ExcludeCategories, v => config.ExcludeCategories = v);
            y += gap;
            y = ProcessDefUiUtility.DrawIntRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.MaxDescriptionLength".Translate(), config.MaxDescriptionLength, v => config.MaxDescriptionLength = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.WeightMarketValueLog10".Translate(), config.ImportanceWeightMarketValueLog10, v => config.ImportanceWeightMarketValueLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.WeightMassLog10".Translate(), config.ImportanceWeightMassLog10, v => config.ImportanceWeightMassLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Thing.WeightStackLimitIsOne".Translate(), config.ImportanceWeightStackLimitIsOne, v => config.ImportanceWeightStackLimitIsOne = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig ?? (ThingProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            HashSet<string> include = BuildFilter(typed.IncludeCategories);
            HashSet<string> exclude = BuildFilter(typed.ExcludeCategories);

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
                if (include.Count > 0 && !include.Contains(category))
                {
                    continue;
                }
                if (exclude.Contains(category))
                {
                    continue;
                }

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

                float marketValue = def.BaseMarketValue;
                float mass = def.BaseMass;
                int stackLimit = def.stackLimit;
                string techLevel = def.techLevel.ToString();

                if (typed.IncludeStatSummary)
                {
                    description = string.IsNullOrWhiteSpace(description)
                        ? $"value={marketValue:0.##}, mass={mass:0.##}, stack={stackLimit}"
                        : description + $" (value={marketValue:0.##}, mass={mass:0.##}, stack={stackLimit})";
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["category"] = category,
                    ["marketValue"] = marketValue.ToString("0.##"),
                    ["mass"] = mass.ToString("0.##"),
                    ["stackLimit"] = stackLimit.ToString(),
                    ["techLevel"] = techLevel,
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float marketMetric = ProcessDefUtility.SafeLog10(marketValue);
                float massMetric = ProcessDefUtility.SafeLog10(mass);
                float stackMetric = stackLimit == 1 ? 1f : 0f;
                float raw = typed.BaseImportance
                    + marketMetric * typed.ImportanceWeightMarketValueLog10
                    + Math.Abs(massMetric) * typed.ImportanceWeightMassLog10
                    + Math.Abs(stackMetric) * typed.ImportanceWeightStackLimitIsOne;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "thing:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static HashSet<string> BuildFilter(string commaSeparated)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(commaSeparated))
            {
                return set;
            }

            string[] tokens = commaSeparated.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string value = token.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    set.Add(value);
                }
            }

            return set;
        }
    }
}



