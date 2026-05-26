using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class TraitDefProcessor : ProcessDefProcessorBase<TraitProcessDefConfig>
    {
        public const string ProcessorId = "TraitDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Trait.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new TraitProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}}",
                BaseImportance = 0.6f,
                ImportanceMin = 0.1f,
                ImportanceMax = 0.9f,
                IncludeDegreeDetails = true,
                ImportanceWeightDegreeCount = 0.01f,
                ImportanceWeightCommonality = 0f,
                ImportanceWeightCommonalityLog10 = -0.1f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            TraitProcessDefConfig typed = config as TraitProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            TraitProcessDefConfig defaults = (TraitProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludeDegreeDetails = defaults.IncludeDegreeDetails;
            typed.ImportanceWeightDegreeCount = defaults.ImportanceWeightDegreeCount;
            typed.ImportanceWeightCommonality = defaults.ImportanceWeightCommonality;
            typed.ImportanceWeightCommonalityLog10 = defaults.ImportanceWeightCommonalityLog10;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Trait label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Trait description" },
                new PlaceholderDescriptor { Key = "degree", Token = "{{degree}}", Description = "Trait degree" },
                new PlaceholderDescriptor { Key = "commonality", Token = "{{commonality}}", Description = "Commonality" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 530f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, TraitProcessDefConfig config)
        {
            Rect degreeRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(degreeRect, "RimTalkGenKnowledge.Settings.Trait.IncludeDegreeDetails".Translate(), ref config.IncludeDegreeDetails);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Trait.WeightDegreeCount".Translate(), config.ImportanceWeightDegreeCount, v => config.ImportanceWeightDegreeCount = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Trait.WeightCommonality".Translate(), config.ImportanceWeightCommonality, v => config.ImportanceWeightCommonality = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Trait.WeightCommonalityLog10".Translate(), config.ImportanceWeightCommonalityLog10, v => config.ImportanceWeightCommonalityLog10 = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            TraitProcessDefConfig typed = config as TraitProcessDefConfig ?? (TraitProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            foreach (TraitDef def in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

                if (!ProcessDefUtility.ShouldIncludeDef(def, typed.IncludeModDefs))
                {
                    continue;
                }

                List<TraitDegreeData> degreeDatas = def.degreeDatas;
                int degreeCount = degreeDatas?.Count ?? 0;
                float commonality = ResolveCommonality(def, degreeDatas);

                if (typed.IncludeDegreeDetails && degreeCount > 0)
                {
                    for (int i = 0; i < degreeDatas.Count; i++)
                    {
                        TraitDegreeData degree = degreeDatas[i];
                        if (degree == null)
                        {
                            continue;
                        }

                        string label = ProcessDefUtility.TrimOrNull(degree.label) ?? ProcessDefUtility.TrimOrNull(def.label);
                        string description = ProcessDefUtility.TrimOrNull(degree.description) ?? ProcessDefUtility.TrimOrNull(def.description);
                        label = ReplacePawnTokens(label);
                        description = ReplacePawnTokens(description);
                        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(description))
                        {
                            continue;
                        }

                        yield return BuildItem(def, typed, label, description, degree.degree.ToString(), commonality, degreeCount);
                    }

                    continue;
                }

                string baseLabel = ProcessDefUtility.TrimOrNull(def.label);
                string baseDescription = ProcessDefUtility.TrimOrNull(def.description);
                baseLabel = ReplacePawnTokens(baseLabel);
                baseDescription = ReplacePawnTokens(baseDescription);
                if (string.IsNullOrWhiteSpace(baseLabel) || string.IsNullOrWhiteSpace(baseDescription))
                {
                    continue;
                }

                yield return BuildItem(def, typed, baseLabel, baseDescription, "0", commonality, degreeCount);
            }
        }

        private GeneratedKnowledgeItem BuildItem(TraitDef def, TraitProcessDefConfig config, string label, string description, string degree, float commonality, int degreeCount)
        {
            SetTemplateValues(new Dictionary<string, string>
            {
                ["label"] = label,
                ["description"] = description,
                ["degree"] = degree,
                ["commonality"] = commonality.ToString("0.####"),
                ["defName"] = def.defName
            });

            string tag = RenderTag(config);
            string content = RenderContent(config);

            float commonalityLogMetric = ProcessDefUtility.SafeLog10(commonality);
            float raw = config.BaseImportance
                + Math.Abs(commonality) * config.ImportanceWeightCommonality
                + Math.Abs(commonalityLogMetric) * config.ImportanceWeightCommonalityLog10
                + Math.Abs(degreeCount) * config.ImportanceWeightDegreeCount;

            return new GeneratedKnowledgeItem
            {
                LogicalKey = "trait:" + def.defName + ":" + degree,
                Tag = tag,
                Content = content,
                Importance = ComputeFinalImportance(raw, config)
            };
        }

        private static float ResolveCommonality(TraitDef def, List<TraitDegreeData> degreeDatas)
        {
            float direct = ProcessDefUtility.GetFloatMemberOrDefault(def, "commonality", 0f);
            if (direct > 0f)
            {
                return direct;
            }

            if (degreeDatas == null || degreeDatas.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            int count = 0;
            for (int i = 0; i < degreeDatas.Count; i++)
            {
                TraitDegreeData degree = degreeDatas[i];
                if (degree == null)
                {
                    continue;
                }

                float value = ProcessDefUtility.GetFloatMemberOrDefault(degree, "commonality", 0f);
                if (value <= 0f)
                {
                    continue;
                }

                sum += value;
                count++;
            }

            return count > 0 ? (sum / count) : 0f;
        }

        private static string ReplacePawnTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return text
                .Replace("PAWN_nameDef", "此人")
                .Replace("PAWN_pronoun", "此人");
        }
    }
}
