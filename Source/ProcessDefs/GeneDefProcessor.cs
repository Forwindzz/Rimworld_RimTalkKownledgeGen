using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class GeneDefProcessor : ProcessDefProcessorBase<GeneProcessDefConfig>
    {
        public const string ProcessorId = "GeneDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Gene.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new GeneProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}} (cpx={{complexity}}, met={{metabolism}}, arc={{archites}})",
                BaseImportance = 0.2f,
                ImportanceMin = 0.05f,
                ImportanceMax = 0.8f,
                IncludeArchiteOnly = false,
                IncludeNegativeGenes = true,
                ImportanceWeightBiostatCpx = 0.03f,
                ImportanceWeightBiostatMet = 0.04f,
                ImportanceWeightBiostatArc = 0.15f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            GeneProcessDefConfig typed = config as GeneProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            GeneProcessDefConfig defaults = (GeneProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludeArchiteOnly = defaults.IncludeArchiteOnly;
            typed.IncludeNegativeGenes = defaults.IncludeNegativeGenes;
            typed.ImportanceWeightBiostatCpx = defaults.ImportanceWeightBiostatCpx;
            typed.ImportanceWeightBiostatMet = defaults.ImportanceWeightBiostatMet;
            typed.ImportanceWeightBiostatArc = defaults.ImportanceWeightBiostatArc;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Gene label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Gene description" },
                new PlaceholderDescriptor { Key = "complexity", Token = "{{complexity}}", Description = "Biostat complexity" },
                new PlaceholderDescriptor { Key = "metabolism", Token = "{{metabolism}}", Description = "Biostat metabolism" },
                new PlaceholderDescriptor { Key = "archites", Token = "{{archites}}", Description = "Biostat archites" },
                new PlaceholderDescriptor { Key = "category", Token = "{{category}}", Description = "Display category" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 600f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, GeneProcessDefConfig config)
        {
            Rect architeRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(architeRect, "RimTalkGenKnowledge.Settings.Gene.IncludeArchiteOnly".Translate(), ref config.IncludeArchiteOnly);
            y += lineHeight + gap;

            Rect negativeRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(negativeRect, "RimTalkGenKnowledge.Settings.Gene.IncludeNegativeGenes".Translate(), ref config.IncludeNegativeGenes);
            y += lineHeight + gap;

            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Gene.WeightBiostatCpx".Translate(), config.ImportanceWeightBiostatCpx, v => config.ImportanceWeightBiostatCpx = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Gene.WeightBiostatMet".Translate(), config.ImportanceWeightBiostatMet, v => config.ImportanceWeightBiostatMet = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Gene.WeightBiostatArc".Translate(), config.ImportanceWeightBiostatArc, v => config.ImportanceWeightBiostatArc = v);
            y += gap;

            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            GeneProcessDefConfig typed = config as GeneProcessDefConfig ?? (GeneProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled || !ModsConfig.BiotechActive)
            {
                yield break;
            }

            foreach (GeneDef def in DefDatabase<GeneDef>.AllDefsListForReading)
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
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                int cpx = def.biostatCpx;
                int met = def.biostatMet;
                int arc = def.biostatArc;

                if (typed.IncludeArchiteOnly && arc <= 0)
                {
                    continue;
                }

                bool isNegativeGene = met < 0;
                if (!typed.IncludeNegativeGenes && isNegativeGene)
                {
                    continue;
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description ?? string.Empty,
                    ["complexity"] = cpx.ToString(),
                    ["metabolism"] = met.ToString(),
                    ["archites"] = arc.ToString(),
                    ["category"] = ProcessDefUtility.TrimOrNull(def.displayCategory?.label) ?? string.Empty,
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float raw = typed.BaseImportance
                    + Math.Abs(cpx) * typed.ImportanceWeightBiostatCpx
                    + Math.Abs(met) * typed.ImportanceWeightBiostatMet
                    + Math.Abs(arc) * typed.ImportanceWeightBiostatArc;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "gene:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }
    }
}
