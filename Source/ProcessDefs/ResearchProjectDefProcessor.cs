using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class ResearchProjectDefProcessor : ProcessDefProcessorBase<ResearchProjectProcessDefConfig>
    {
        public const string ProcessorId = "ResearchProjectDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Research.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new ResearchProjectProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Research.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Research.Knowledge".Translate(),
                BaseImportance = 0.1f,
                ImportanceMin = 0.01f,
                ImportanceMax = 0.83f,
                IncludePrerequisites = true,
                IncludePostrequisites = true,
                ImportanceWeightCost = 0.1f,
                ImportanceWeightPrereqCount = 0.03f,
                ImportanceWeightPostreqCount = 0.04f,
                UseCostLog10Weight = true
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            ResearchProjectProcessDefConfig typed = config as ResearchProjectProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            ResearchProjectProcessDefConfig defaults = (ResearchProjectProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludePrerequisites = defaults.IncludePrerequisites;
            typed.IncludePostrequisites = defaults.IncludePostrequisites;
            typed.ImportanceWeightCost = defaults.ImportanceWeightCost;
            typed.ImportanceWeightPrereqCount = defaults.ImportanceWeightPrereqCount;
            typed.ImportanceWeightPostreqCount = defaults.ImportanceWeightPostreqCount;
            typed.UseCostLog10Weight = defaults.UseCostLog10Weight;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Research label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Research description" },
                new PlaceholderDescriptor { Key = "cost", Token = "{{cost}}", Description = "Research cost" },
                new PlaceholderDescriptor { Key = "costDifficulty", Token = "{{costDifficulty}}", Description = "Cost difficulty label" },
                new PlaceholderDescriptor { Key = "researchDifficulty", Token = "{{researchDifficulty}}", Description = "Research difficulty label" },
                new PlaceholderDescriptor { Key = "costDifficultyLine", Token = "{{costDifficultyLine}}", Description = "Cost difficulty line" },
                new PlaceholderDescriptor { Key = "techLevel", Token = "{{techLevel}}", Description = "Tech level" },
                new PlaceholderDescriptor { Key = "prerequisites", Token = "{{prerequisites}}", Description = "Prerequisite projects" },
                new PlaceholderDescriptor { Key = "postrequisites", Token = "{{postrequisites}}", Description = "Post-requisite projects" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 560f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, ResearchProjectProcessDefConfig config)
        {
            Rect preRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(preRect, "RimTalkGenKnowledge.Settings.Research.IncludePrerequisites".Translate(), ref config.IncludePrerequisites);
            y += lineHeight + gap;
            Rect postRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(postRect, "RimTalkGenKnowledge.Settings.Research.IncludePostrequisites".Translate(), ref config.IncludePostrequisites);
            y += lineHeight + gap;
            Rect logRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(logRect, "RimTalkGenKnowledge.Settings.Research.UseCostLog10Weight".Translate(), ref config.UseCostLog10Weight);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Research.WeightCost".Translate(), config.ImportanceWeightCost, v => config.ImportanceWeightCost = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Research.WeightPrereqCount".Translate(), config.ImportanceWeightPrereqCount, v => config.ImportanceWeightPrereqCount = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Research.WeightPostreqCount".Translate(), config.ImportanceWeightPostreqCount, v => config.ImportanceWeightPostreqCount = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            ResearchProjectProcessDefConfig typed = config as ResearchProjectProcessDefConfig ?? (ResearchProjectProcessDefConfig)CreateDefaultConfig();
            bool showNumericValues = context?.ShowNumericValues ?? false;
            if (!typed.Enabled)
            {
                yield break;
            }

            List<ResearchProjectDef> allDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.defName))
                .ToList();

            Dictionary<ResearchProjectDef, List<ResearchProjectDef>> postreqMap = BuildPostrequisiteMap(allDefs);

            foreach (ResearchProjectDef def in allDefs)
            {
                if (!ProcessDefUtility.ShouldIncludeDef(def, typed.IncludeModDefs))
                {
                    continue;
                }

                string label = ProcessDefUtility.TrimOrNull(def.label);
                string description = ProcessDefUtility.TrimOrNull(def.description);
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                int prereqCount = def.prerequisites?.Count ?? 0;
                string prerequisites = string.Empty;
                if (typed.IncludePrerequisites && prereqCount > 0)
                {
                    prerequisites = string.Join(",", def.prerequisites.Select(p => p?.label ?? p?.defName).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());
                }

                postreqMap.TryGetValue(def, out List<ResearchProjectDef> postreqDefs);
                int postreqCount = postreqDefs?.Count ?? 0;
                string postrequisites = string.Empty;
                if (typed.IncludePostrequisites && postreqCount > 0)
                {
                    postrequisites = string.Join(",", postreqDefs.Select(p => p?.label ?? p?.defName).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());
                }

                float cost = def.baseCost;
                string costDifficulty = GetCostDifficultyLabel(cost);
                string researchDifficulty = string.IsNullOrWhiteSpace(costDifficulty)
                    ? "RimTalkGenKnowledge.Text.Research.CostDifficulty.Normal".Translate().ToString()
                    : costDifficulty;
                string costDifficultyLine = (!showNumericValues || string.IsNullOrWhiteSpace(costDifficulty))
                    ? string.Empty
                    : "\n" + string.Format("RimTalkGenKnowledge.Text.Research.CostDifficultyLine".Translate(), costDifficulty);
                string costDisplay = showNumericValues
                    ? cost.ToString("0.##")
                    : researchDifficulty;
                string techLevelText = LocalizeTechLevel(def.techLevel);
                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["cost"] = costDisplay,
                    ["costDifficulty"] = costDifficulty,
                    ["researchDifficulty"] = researchDifficulty,
                    ["costDifficultyLine"] = costDifficultyLine,
                    ["techLevel"] = techLevelText,
                    ["prerequisites"] = prerequisites,
                    ["postrequisites"] = postrequisites,
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float costMetric = typed.UseCostLog10Weight ? ProcessDefUtility.SafeLog10(cost) : cost;
                float raw = typed.BaseImportance
                    + Math.Abs(costMetric) * typed.ImportanceWeightCost
                    + Math.Abs(prereqCount) * typed.ImportanceWeightPrereqCount
                    + Math.Abs(postreqCount) * typed.ImportanceWeightPostreqCount;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "research:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static Dictionary<ResearchProjectDef, List<ResearchProjectDef>> BuildPostrequisiteMap(List<ResearchProjectDef> allDefs)
        {
            var map = new Dictionary<ResearchProjectDef, List<ResearchProjectDef>>();
            if (allDefs == null)
            {
                return map;
            }

            foreach (ResearchProjectDef def in allDefs)
            {
                if (def?.prerequisites == null)
                {
                    continue;
                }

                foreach (ResearchProjectDef prereq in def.prerequisites)
                {
                    if (prereq == null)
                    {
                        continue;
                    }

                    if (!map.TryGetValue(prereq, out List<ResearchProjectDef> dependents))
                    {
                        dependents = new List<ResearchProjectDef>();
                        map[prereq] = dependents;
                    }

                    if (!dependents.Contains(def))
                    {
                        dependents.Add(def);
                    }
                }
            }

            return map;
        }

        private static string GetCostDifficultyLabel(float cost)
        {
            if (cost < 1000f)
            {
                return "RimTalkGenKnowledge.Text.Research.CostDifficulty.Easy".Translate();
            }

            if (cost > 10000f)
            {
                return "RimTalkGenKnowledge.Text.Research.CostDifficulty.Hard".Translate();
            }

            return string.Empty;
        }

        private static string LocalizeTechLevel(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Animal:
                    return "RimTalkGenKnowledge.Text.TechLevel.Animal".Translate();
                case TechLevel.Neolithic:
                    return "RimTalkGenKnowledge.Text.TechLevel.Neolithic".Translate();
                case TechLevel.Medieval:
                    return "RimTalkGenKnowledge.Text.TechLevel.Medieval".Translate();
                case TechLevel.Industrial:
                    return "RimTalkGenKnowledge.Text.TechLevel.Industrial".Translate();
                case TechLevel.Spacer:
                    return "RimTalkGenKnowledge.Text.TechLevel.Spacer".Translate();
                case TechLevel.Ultra:
                    return "RimTalkGenKnowledge.Text.TechLevel.Ultra".Translate();
                case TechLevel.Archotech:
                    return "RimTalkGenKnowledge.Text.TechLevel.Archotech".Translate();
                case TechLevel.Undefined:
                default:
                    return "RimTalkGenKnowledge.Text.TechLevel.Undefined".Translate();
            }
        }
    }
}
