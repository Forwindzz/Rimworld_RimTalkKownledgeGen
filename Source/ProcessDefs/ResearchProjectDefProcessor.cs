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
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}} (cost={{cost}}, tech={{techLevel}})",
                BaseImportance = 0.1f,
                ImportanceMin = 0.01f,
                ImportanceMax = 0.83f,
                IncludePrerequisites = true,
                ImportanceWeightCost = 0.1f,
                ImportanceWeightPrereqCount = 0.03f,
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
            typed.ImportanceWeightCost = defaults.ImportanceWeightCost;
            typed.ImportanceWeightPrereqCount = defaults.ImportanceWeightPrereqCount;
            typed.UseCostLog10Weight = defaults.UseCostLog10Weight;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Research label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Research description" },
                new PlaceholderDescriptor { Key = "cost", Token = "{{cost}}", Description = "Research cost" },
                new PlaceholderDescriptor { Key = "techLevel", Token = "{{techLevel}}", Description = "Tech level" },
                new PlaceholderDescriptor { Key = "prerequisites", Token = "{{prerequisites}}", Description = "Prerequisite projects" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 520f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, ResearchProjectProcessDefConfig config)
        {
            Rect preRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(preRect, "RimTalkGenKnowledge.Settings.Research.IncludePrerequisites".Translate(), ref config.IncludePrerequisites);
            y += lineHeight + gap;
            Rect logRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(logRect, "RimTalkGenKnowledge.Settings.Research.UseCostLog10Weight".Translate(), ref config.UseCostLog10Weight);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Research.WeightCost".Translate(), config.ImportanceWeightCost, v => config.ImportanceWeightCost = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Research.WeightPrereqCount".Translate(), config.ImportanceWeightPrereqCount, v => config.ImportanceWeightPrereqCount = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            ResearchProjectProcessDefConfig typed = config as ResearchProjectProcessDefConfig ?? (ResearchProjectProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            foreach (ResearchProjectDef def in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

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

                float cost = def.baseCost;
                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["cost"] = cost.ToString("0.##"),
                    ["techLevel"] = def.techLevel.ToString(),
                    ["prerequisites"] = prerequisites,
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
                    + Math.Abs(prereqCount) * typed.ImportanceWeightPrereqCount;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "research:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }
    }
}
