using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class HediffDefProcessor : ProcessDefProcessorBase<HediffProcessDefConfig>
    {
        public const string ProcessorId = "HediffDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Hediff.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new HediffProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}: {{description}} (bad={{isBad}}, chronic={{isChronic}}, tendable={{tendable}})",
                BaseImportance = 0.6f,
                ImportanceMin = 0.1f,
                ImportanceMax = 0.88f,
                IncludeGoodHediffs = true,
                IncludeImplants = true,
                ImportanceWeightIsBad = 0.05f,
                ImportanceWeightIsChronic = 0.10f,
                ImportanceWeightTendable = -0.03f,
                ImportanceWeightIsLethal = 0.15f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            HediffProcessDefConfig typed = config as HediffProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            HediffProcessDefConfig defaults = (HediffProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludeGoodHediffs = defaults.IncludeGoodHediffs;
            typed.IncludeImplants = defaults.IncludeImplants;
            typed.ImportanceWeightIsBad = defaults.ImportanceWeightIsBad;
            typed.ImportanceWeightIsChronic = defaults.ImportanceWeightIsChronic;
            typed.ImportanceWeightTendable = defaults.ImportanceWeightTendable;
            typed.ImportanceWeightIsLethal = defaults.ImportanceWeightIsLethal;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Hediff label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Hediff description" },
                new PlaceholderDescriptor { Key = "isBad", Token = "{{isBad}}", Description = "Whether this is harmful" },
                new PlaceholderDescriptor { Key = "isChronic", Token = "{{isChronic}}", Description = "Whether this is chronic" },
                new PlaceholderDescriptor { Key = "tendable", Token = "{{tendable}}", Description = "Whether this can be tended" },
                new PlaceholderDescriptor { Key = "isLethal", Token = "{{isLethal}}", Description = "Whether this may be lethal" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 600f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, HediffProcessDefConfig config)
        {
            Rect includeGoodRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(includeGoodRect, "RimTalkGenKnowledge.Settings.Hediff.IncludeGoodHediffs".Translate(), ref config.IncludeGoodHediffs);
            y += lineHeight + gap;

            Rect includeImplantRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(includeImplantRect, "RimTalkGenKnowledge.Settings.Hediff.IncludeImplants".Translate(), ref config.IncludeImplants);
            y += lineHeight + gap;

            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Hediff.WeightIsBad".Translate(), config.ImportanceWeightIsBad, v => config.ImportanceWeightIsBad = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Hediff.WeightIsChronic".Translate(), config.ImportanceWeightIsChronic, v => config.ImportanceWeightIsChronic = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Hediff.WeightTendable".Translate(), config.ImportanceWeightTendable, v => config.ImportanceWeightTendable = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Hediff.WeightIsLethal".Translate(), config.ImportanceWeightIsLethal, v => config.ImportanceWeightIsLethal = v);
            y += gap;

            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            HediffProcessDefConfig typed = config as HediffProcessDefConfig ?? (HediffProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            foreach (HediffDef def in DefDatabase<HediffDef>.AllDefsListForReading)
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

                bool isBad = def.isBad;
                bool isChronic = def.chronic;
                bool tendable = def.tendable;
                bool isLethal = def.lethalSeverity > 0f;
                bool isImplant = IsImplant(def);

                if (!typed.IncludeGoodHediffs && !isBad)
                {
                    continue;
                }

                if (!typed.IncludeImplants && isImplant)
                {
                    continue;
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description ?? string.Empty,
                    ["isBad"] = isBad ? "true" : "false",
                    ["isChronic"] = isChronic ? "true" : "false",
                    ["tendable"] = tendable ? "true" : "false",
                    ["isLethal"] = isLethal ? "true" : "false",
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float raw = typed.BaseImportance
                    + Math.Abs(isBad ? 1f : 0f) * typed.ImportanceWeightIsBad
                    + Math.Abs(isChronic ? 1f : 0f) * typed.ImportanceWeightIsChronic
                    + Math.Abs(tendable ? 1f : 0f) * typed.ImportanceWeightTendable
                    + Math.Abs(isLethal ? 1f : 0f) * typed.ImportanceWeightIsLethal;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "hediff:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static bool IsImplant(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "isAddedPartOrImplant", out object flagValue))
            {
                try
                {
                    return Convert.ToBoolean(flagValue);
                }
                catch
                {
                }
            }

            return ProcessDefUtility.TryGetMemberValue(def, "addedPartProps", out object propsValue) && propsValue != null;
        }
    }
}
