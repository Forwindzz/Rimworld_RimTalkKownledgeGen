using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class FactionDefProcessor : ProcessDefProcessorBase<FactionProcessDefConfig>
    {
        public const string ProcessorId = "FactionDefProcessor";

        private static readonly Dictionary<string, float> DefaultTechLevelMap = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["Undefined"] = 0f,
            ["Animal"] = 1f,
            ["Neolithic"] = 2f,
            ["Medieval"] = 3f,
            ["Industrial"] = 4f,
            ["Spacer"] = 5f,
            ["Ultra"] = 6f,
            ["Archotech"] = 7f
        };

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Faction.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new FactionProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "{{label}}",
                KnowledgeTemplate = "{{label}}：{{description}}\n技术水平：{{techLevel}}\n是否永久敌对：{{permanentEnemy}}\n是否类人：{{humanlike}}",
                BaseImportance = 0.5f,
                ImportanceMin = 0.2f,
                ImportanceMax = 0.9f,
                IncludePlayerFaction = false,
                IncludeHiddenFactions = false,
                ImportanceWeightTechLevel = 0.05f,
                ImportanceWeightPermanentEnemy = 0.075f,
                ImportanceWeightHumanlike = 0.03f,
                TechLevelScoreMap = "Undefined:0,Animal:1,Neolithic:2,Medieval:3,Industrial:4,Spacer:5,Ultra:6,Archotech:7"
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            FactionProcessDefConfig typed = config as FactionProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            FactionProcessDefConfig defaults = (FactionProcessDefConfig)CreateDefaultConfig();
            typed.Enabled = defaults.Enabled;
            typed.IncludeModDefs = defaults.IncludeModDefs;
            typed.TagTemplate = defaults.TagTemplate;
            typed.KnowledgeTemplate = defaults.KnowledgeTemplate;
            typed.BaseImportance = defaults.BaseImportance;
            typed.ImportanceMin = defaults.ImportanceMin;
            typed.ImportanceMax = defaults.ImportanceMax;
            typed.IncludePlayerFaction = defaults.IncludePlayerFaction;
            typed.IncludeHiddenFactions = defaults.IncludeHiddenFactions;
            typed.ImportanceWeightTechLevel = defaults.ImportanceWeightTechLevel;
            typed.ImportanceWeightPermanentEnemy = defaults.ImportanceWeightPermanentEnemy;
            typed.ImportanceWeightHumanlike = defaults.ImportanceWeightHumanlike;
            typed.TechLevelScoreMap = defaults.TechLevelScoreMap;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Faction label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Faction description" },
                new PlaceholderDescriptor { Key = "techLevel", Token = "{{techLevel}}", Description = "Tech level" },
                new PlaceholderDescriptor { Key = "humanlike", Token = "{{humanlike}}", Description = "Humanlike faction" },
                new PlaceholderDescriptor { Key = "permanentEnemy", Token = "{{permanentEnemy}}", Description = "Permanent enemy faction" },
                new PlaceholderDescriptor { Key = "isPlayer", Token = "{{isPlayer}}", Description = "Player faction" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 640f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, FactionProcessDefConfig config)
        {
            Rect includePlayerRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(includePlayerRect, "RimTalkGenKnowledge.Settings.Faction.IncludePlayerFaction".Translate(), ref config.IncludePlayerFaction);
            y += lineHeight + gap;

            Rect includeHiddenRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(includeHiddenRect, "RimTalkGenKnowledge.Settings.Faction.IncludeHiddenFactions".Translate(), ref config.IncludeHiddenFactions);
            y += lineHeight + gap;

            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Faction.WeightTechLevel".Translate(), config.ImportanceWeightTechLevel, v => config.ImportanceWeightTechLevel = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Faction.WeightPermanentEnemy".Translate(), config.ImportanceWeightPermanentEnemy, v => config.ImportanceWeightPermanentEnemy = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Faction.WeightHumanlike".Translate(), config.ImportanceWeightHumanlike, v => config.ImportanceWeightHumanlike = v);
            y += gap;
            y = ProcessDefUiUtility.DrawTextRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Faction.TechLevelScoreMap".Translate(), config.TechLevelScoreMap, v => config.TechLevelScoreMap = v);
            y += gap;

            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            FactionProcessDefConfig typed = config as FactionProcessDefConfig ?? (FactionProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            Dictionary<string, float> techMap = ProcessDefUtility.ParseKeyFloatMap(typed.TechLevelScoreMap, DefaultTechLevelMap);

            foreach (FactionDef def in DefDatabase<FactionDef>.AllDefsListForReading)
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
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                bool isPlayer = ProcessDefUtility.GetBoolMemberOrDefault(def, "isPlayer", false);
                bool isHidden = ProcessDefUtility.GetBoolMemberOrDefault(def, "hidden", false);
                if (!typed.IncludePlayerFaction && isPlayer)
                {
                    continue;
                }

                if (!typed.IncludeHiddenFactions && isHidden)
                {
                    continue;
                }

                string techLevel = def.techLevel.ToString();
                float techMetric = techMap.TryGetValue(techLevel, out float mapped) ? mapped : 0f;
                bool permanentEnemy = def.permanentEnemy;
                bool humanlike = def.humanlikeFaction;
                string description = ProcessDefUtility.TrimOrNull(def.description);
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = "技术水平：" + techLevel
                        + "，永久敌对：" + (permanentEnemy ? "是" : "否")
                        + "，类人派系：" + (humanlike ? "是" : "否");
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["techLevel"] = techLevel,
                    ["humanlike"] = humanlike ? "true" : "false",
                    ["permanentEnemy"] = permanentEnemy ? "true" : "false",
                    ["isPlayer"] = isPlayer ? "true" : "false",
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float raw = typed.BaseImportance
                    + Math.Abs(techMetric) * typed.ImportanceWeightTechLevel
                    + Math.Abs(permanentEnemy ? 1f : 0f) * typed.ImportanceWeightPermanentEnemy
                    + Math.Abs(humanlike ? 1f : 0f) * typed.ImportanceWeightHumanlike;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "faction:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }
    }
}
