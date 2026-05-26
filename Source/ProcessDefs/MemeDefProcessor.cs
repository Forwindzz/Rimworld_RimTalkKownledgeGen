using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class MemeDefProcessor : ProcessDefProcessorBase<MemeProcessDefConfig>
    {
        public const string ProcessorId = "MemeDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Meme.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new MemeProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Meme.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Meme.Knowledge".Translate(),
                BaseImportance = 0.5f,
                ImportanceMin = 0.05f,
                ImportanceMax = 0.82f,
                IncludeStructureMemes = true,
                IncludeJokeMemes = false,
                ImportanceWeightImpact = 0.1f,
                ImportanceWeightCategory = 0.02f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            MemeProcessDefConfig typed = config as MemeProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            MemeProcessDefConfig defaults = (MemeProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludeStructureMemes = defaults.IncludeStructureMemes;
            typed.IncludeJokeMemes = defaults.IncludeJokeMemes;
            typed.ImportanceWeightImpact = defaults.ImportanceWeightImpact;
            typed.ImportanceWeightCategory = defaults.ImportanceWeightCategory;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Meme label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Meme description" },
                new PlaceholderDescriptor { Key = "category", Token = "{{category}}", Description = "Meme category" },
                new PlaceholderDescriptor { Key = "impact", Token = "{{impact}}", Description = "Meme impact" },
                new PlaceholderDescriptor { Key = "categoryLine", Token = "{{categoryLine}}", Description = "Category line, hidden when Normal" },
                new PlaceholderDescriptor { Key = "impactLabel", Token = "{{impactLabel}}", Description = "Impact label text" },
                new PlaceholderDescriptor { Key = "impactLinePrefix", Token = "{{impactLinePrefix}}", Description = "Impact line prefix" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 570f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, MemeProcessDefConfig config)
        {
            Rect structureRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(structureRect, "RimTalkGenKnowledge.Settings.Meme.IncludeStructureMemes".Translate(), ref config.IncludeStructureMemes);
            y += lineHeight + gap;

            Rect jokeRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(jokeRect, "RimTalkGenKnowledge.Settings.Meme.IncludeJokeMemes".Translate(), ref config.IncludeJokeMemes);
            y += lineHeight + gap;

            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Meme.WeightImpact".Translate(), config.ImportanceWeightImpact, v => config.ImportanceWeightImpact = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Meme.WeightCategory".Translate(), config.ImportanceWeightCategory, v => config.ImportanceWeightCategory = v);
            y += gap;

            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            MemeProcessDefConfig typed = config as MemeProcessDefConfig ?? (MemeProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled || !ModsConfig.IdeologyActive)
            {
                yield break;
            }

            foreach (MemeDef def in DefDatabase<MemeDef>.AllDefsListForReading)
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

                string category = ProcessDefUtility.GetStringMemberOrDefault(def, "category", string.Empty);
                string categoryLower = category?.ToLowerInvariant() ?? string.Empty;

                bool isJoke = categoryLower.Contains("joke") || def.defName.ToLowerInvariant().Contains("joke");
                bool isStructure = categoryLower.Contains("structure");
                if (!typed.IncludeJokeMemes && isJoke)
                {
                    continue;
                }

                if (!typed.IncludeStructureMemes && isStructure)
                {
                    continue;
                }

                float impact = ResolveImpactMetric(ProcessDefUtility.GetStringMemberOrDefault(def, "impact", string.Empty));
                string impactLabel = ResolveImpactLabel(impact);
                bool isNormalCategory = string.Equals(category, "Normal", StringComparison.OrdinalIgnoreCase);
                string categoryLine = isNormalCategory || string.IsNullOrWhiteSpace(category)
                    ? string.Empty
                    : ("\n" + "RimTalkGenKnowledge.Text.Meme.CategoryPrefix".Translate().ToString() + category);

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description ?? string.Empty,
                    ["category"] = category ?? string.Empty,
                    ["impact"] = impact.ToString("0.##"),
                    ["categoryLine"] = categoryLine,
                    ["impactLabel"] = impactLabel,
                    ["impactLinePrefix"] = "RimTalkGenKnowledge.Text.Meme.ImpactPrefix".Translate(),
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                bool hasCategory = !string.IsNullOrWhiteSpace(category) && !isNormalCategory;
                float raw = typed.BaseImportance
                    + Math.Abs(impact) * typed.ImportanceWeightImpact
                    + Math.Abs(hasCategory ? 1f : 0f) * typed.ImportanceWeightCategory;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "meme:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static float ResolveImpactMetric(string rawImpact)
        {
            if (string.IsNullOrWhiteSpace(rawImpact))
            {
                return 0f;
            }

            string text = rawImpact.Trim();
            if (float.TryParse(text, out float numeric))
            {
                return numeric;
            }

            string normalized = text.ToLowerInvariant();
            if (normalized.Contains("low"))
            {
                return 1f;
            }

            if (normalized.Contains("medium"))
            {
                return 2f;
            }

            if (normalized.Contains("high"))
            {
                return 3f;
            }

            return 0f;
        }

        private static string ResolveImpactLabel(float impact)
        {
            if (impact <= 1f)
            {
                return "RimTalkGenKnowledge.Text.Meme.Impact.Low".Translate();
            }

            if (Mathf.Approximately(impact, 2f))
            {
                return "RimTalkGenKnowledge.Text.Meme.Impact.Mid".Translate();
            }

            return "RimTalkGenKnowledge.Text.Meme.Impact.High".Translate();
        }
    }
}
