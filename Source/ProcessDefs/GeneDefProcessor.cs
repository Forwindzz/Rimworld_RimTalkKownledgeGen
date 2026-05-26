using System;
using System.Collections.Generic;
using System.Linq;
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
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Gene.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Gene.Knowledge".Translate(),
                BaseImportance = 0.2f,
                ImportanceMin = 0.05f,
                ImportanceMax = 0.8f,
                IncludeArchiteOnly = false,
                IncludeNegativeGenes = true,
                ImportanceWeightBiostatCpx = 0.03f,
                ImportanceWeightBiostatMet = 0.04f,
                ImportanceWeightBiostatArc = 0.09f,
                ImportanceWeightDescriptionLengthLog10 = 0.04f,
                ImportanceBonusHasAbilities = 0.15f
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
            CopyBaseConfigFields(defaults, typed);
            typed.IncludeArchiteOnly = defaults.IncludeArchiteOnly;
            typed.IncludeNegativeGenes = defaults.IncludeNegativeGenes;
            typed.ImportanceWeightBiostatCpx = defaults.ImportanceWeightBiostatCpx;
            typed.ImportanceWeightBiostatMet = defaults.ImportanceWeightBiostatMet;
            typed.ImportanceWeightBiostatArc = defaults.ImportanceWeightBiostatArc;
            typed.ImportanceWeightDescriptionLengthLog10 = defaults.ImportanceWeightDescriptionLengthLog10;
            typed.ImportanceBonusHasAbilities = defaults.ImportanceBonusHasAbilities;
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
                new PlaceholderDescriptor { Key = "complexityLine", Token = "{{complexityLine}}", Description = "Complexity summary line" },
                new PlaceholderDescriptor { Key = "metabolismLine", Token = "{{metabolismLine}}", Description = "Metabolism summary line" },
                new PlaceholderDescriptor { Key = "architesLine", Token = "{{architesLine}}", Description = "Archites summary line" },
                new PlaceholderDescriptor { Key = "geneStats", Token = "{{geneStats}}", Description = "Joined gene stats text" },
                new PlaceholderDescriptor { Key = "geneStatsLine", Token = "{{geneStatsLine}}", Description = "Joined gene stats with leading newline when non-empty" },
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
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Gene.WeightDescriptionLengthLog10".Translate(), config.ImportanceWeightDescriptionLengthLog10, v => config.ImportanceWeightDescriptionLengthLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Gene.BonusHasAbilities".Translate(), config.ImportanceBonusHasAbilities, v => config.ImportanceBonusHasAbilities = v);
            y += gap;

            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            GeneProcessDefConfig typed = config as GeneProcessDefConfig ?? (GeneProcessDefConfig)CreateDefaultConfig();
            bool showNumericValues = context?.ShowNumericValues ?? false;
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
                bool hasAbilities = def.abilities != null && def.abilities.Count > 0;

                if (typed.IncludeArchiteOnly && arc <= 0)
                {
                    continue;
                }

                bool isNegativeGene = met < 0;
                if (!typed.IncludeNegativeGenes && isNegativeGene)
                {
                    continue;
                }

                string complexityLine = BuildComplexityLine(cpx, showNumericValues);
                string metabolismLine = BuildMetabolismLine(met, showNumericValues);
                string architesLine = BuildArchitesLine(arc, showNumericValues);
                string separator = "RimTalkGenKnowledge.Text.Gene.StatsSeparator".Translate();
                string geneStats = string.Join(separator, new[] { complexityLine, metabolismLine, architesLine }.Where(s => !string.IsNullOrWhiteSpace(s)));
                string geneStatsLine = string.IsNullOrWhiteSpace(geneStats) ? string.Empty : ("\n" + geneStats);

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description ?? string.Empty,
                    ["complexity"] = cpx.ToString(),
                    ["metabolism"] = met.ToString(),
                    ["archites"] = arc.ToString(),
                    ["complexityLine"] = complexityLine,
                    ["metabolismLine"] = metabolismLine,
                    ["architesLine"] = architesLine,
                    ["geneStats"] = geneStats,
                    ["geneStatsLine"] = geneStatsLine,
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
                    + Math.Abs(arc) * typed.ImportanceWeightBiostatArc
                    + ProcessDefUtility.SafeLog10((description ?? string.Empty).Length + 1f) * typed.ImportanceWeightDescriptionLengthLog10
                    + (hasAbilities ? typed.ImportanceBonusHasAbilities : 0f);

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "gene:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static string BuildComplexityLine(int cpx, bool showNumericValues)
        {
            if (cpx == 0)
            {
                return string.Empty;
            }

            string level;
            if (cpx >= 1 && cpx <= 3)
            {
                level = "RimTalkGenKnowledge.Text.Gene.Complexity.Low".Translate();
            }
            else if (cpx >= 4 && cpx <= 10)
            {
                level = "RimTalkGenKnowledge.Text.Gene.Complexity.Mid".Translate();
            }
            else
            {
                level = "RimTalkGenKnowledge.Text.Gene.Complexity.High".Translate();
            }

            if (!showNumericValues)
            {
                return level;
            }

            return string.Format("RimTalkGenKnowledge.Text.Gene.Complexity.Line".Translate(), cpx, level);
        }

        private static string BuildMetabolismLine(int met, bool showNumericValues)
        {
            if (met == 0)
            {
                return string.Empty;
            }

            string summary = string.Empty;
            if (met < -2)
            {
                summary = "RimTalkGenKnowledge.Text.Gene.Metabolism.VeryCostly".Translate();
            }
            else if (met >= -2 && met <= -1)
            {
                summary = "RimTalkGenKnowledge.Text.Gene.Metabolism.Costly".Translate();
            }
            else if (met >= 1 && met <= 2)
            {
                summary = "RimTalkGenKnowledge.Text.Gene.Metabolism.LessCost".Translate();
            }
            else if (met >= 3)
            {
                summary = "RimTalkGenKnowledge.Text.Gene.Metabolism.MuchLessCost".Translate();
            }

            if (!showNumericValues)
            {
                return summary;
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                return string.Format("RimTalkGenKnowledge.Text.Gene.Metabolism.LineRaw".Translate(), met);
            }

            return string.Format("RimTalkGenKnowledge.Text.Gene.Metabolism.LineWithSummary".Translate(), met, summary);
        }

        private static string BuildArchitesLine(int arc, bool showNumericValues)
        {
            if (arc <= 0)
            {
                return string.Empty;
            }

            if (!showNumericValues)
            {
                return "RimTalkGenKnowledge.Text.Gene.Archites.LineNoNumeric".Translate();
            }

            return string.Format("RimTalkGenKnowledge.Text.Gene.Archites.Line".Translate(), arc);
        }
    }
}
