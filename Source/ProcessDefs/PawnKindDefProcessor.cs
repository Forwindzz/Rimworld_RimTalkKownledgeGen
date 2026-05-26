using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class PawnKindDefProcessor : ProcessDefProcessorBase<PawnKindProcessDefConfig>
    {
        public const string ProcessorId = "PawnKindDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.PawnKind.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new PawnKindProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.PawnKind.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.PawnKind.Knowledge".Translate(),
                BaseImportance = 0.5f,
                ImportanceMin = 0.05f,
                ImportanceMax = 0.85f,
                IncludeAnimals = true,
                IncludeMechanoids = true,
                ImportanceWeightCombatPowerLog10 = 0.05f,
                ImportanceWeightTrader = 0.1f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            PawnKindProcessDefConfig typed = config as PawnKindProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            PawnKindProcessDefConfig defaults = (PawnKindProcessDefConfig)CreateDefaultConfig();
            CopyBaseConfigFields(defaults, typed);
            typed.IncludeAnimals = defaults.IncludeAnimals;
            typed.IncludeMechanoids = defaults.IncludeMechanoids;
            typed.ImportanceWeightCombatPowerLog10 = defaults.ImportanceWeightCombatPowerLog10;
            typed.ImportanceWeightTrader = defaults.ImportanceWeightTrader;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Pawn kind label" },
                new PlaceholderDescriptor { Key = "race", Token = "{{race}}", Description = "Race label" },
                new PlaceholderDescriptor { Key = "combatPower", Token = "{{combatPower}}", Description = "Combat power" },
                new PlaceholderDescriptor { Key = "combatPowerLevel", Token = "{{combatPowerLevel}}", Description = "Combat power level label" },
                new PlaceholderDescriptor { Key = "combatPowerLevelSuffix", Token = "{{combatPowerLevelSuffix}}", Description = "Combat power level suffix" },
                new PlaceholderDescriptor { Key = "faction", Token = "{{faction}}", Description = "Default faction" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Pawn kind description" },
                new PlaceholderDescriptor { Key = "descriptionLine", Token = "{{descriptionLine}}", Description = "Description line" },
                new PlaceholderDescriptor { Key = "isTrader", Token = "{{isTrader}}", Description = "Trader flag" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 540f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, PawnKindProcessDefConfig config)
        {
            Rect animalsRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(animalsRect, "RimTalkGenKnowledge.Settings.PawnKind.IncludeAnimals".Translate(), ref config.IncludeAnimals);
            y += lineHeight + gap;
            Rect mechRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(mechRect, "RimTalkGenKnowledge.Settings.PawnKind.IncludeMechanoids".Translate(), ref config.IncludeMechanoids);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.PawnKind.WeightCombatPowerLog10".Translate(), config.ImportanceWeightCombatPowerLog10, v => config.ImportanceWeightCombatPowerLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.PawnKind.WeightTrader".Translate(), config.ImportanceWeightTrader, v => config.ImportanceWeightTrader = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            PawnKindProcessDefConfig typed = config as PawnKindProcessDefConfig ?? (PawnKindProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading)
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

                string description = ProcessDefUtility.TrimOrNull(def.description) ?? string.Empty;
                if (!ProcessDefUtility.MeetsDescriptionLengthThreshold(label, description, 3f))
                {
                    continue;
                }

                bool isAnimal = def.race?.race != null && def.race.race.Animal;
                bool isMechanoid = def.race?.race != null && def.race.race.IsMechanoid;
                if (!typed.IncludeAnimals && isAnimal)
                {
                    continue;
                }
                if (!typed.IncludeMechanoids && isMechanoid)
                {
                    continue;
                }

                float combatPower = def.combatPower;
                string combatPowerLevel = ResolveCombatPowerLevel(combatPower);
                string combatPowerLevelSuffix = string.IsNullOrWhiteSpace(combatPowerLevel)
                    ? string.Empty
                    : string.Format("RimTalkGenKnowledge.Text.PawnKind.CombatPowerSuffix".Translate(), combatPowerLevel);
                bool isTrader = def.trader;
                string factionLabel = ResolveFactionLabel(def);
                string descriptionLine = string.IsNullOrWhiteSpace(description)
                    ? string.Empty
                    : ("\n" + string.Format("RimTalkGenKnowledge.Text.PawnKind.DescriptionLine".Translate(), description));

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["race"] = def.race?.label ?? string.Empty,
                    ["combatPower"] = combatPower.ToString("0.##"),
                    ["combatPowerLevel"] = combatPowerLevel,
                    ["combatPowerLevelSuffix"] = combatPowerLevelSuffix,
                    ["faction"] = factionLabel,
                    ["description"] = description,
                    ["descriptionLine"] = descriptionLine,
                    ["racePrefix"] = "RimTalkGenKnowledge.Text.PawnKind.RacePrefix".Translate(),
                    ["combatPowerPrefix"] = "RimTalkGenKnowledge.Text.PawnKind.CombatPowerPrefix".Translate(),
                    ["factionPrefix"] = "RimTalkGenKnowledge.Text.PawnKind.FactionPrefix".Translate(),
                    ["isTrader"] = isTrader ? "true" : "false",
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float combatMetric = ProcessDefUtility.SafeLog10(combatPower);
                float raw = typed.BaseImportance
                    + Math.Abs(combatMetric) * typed.ImportanceWeightCombatPowerLog10
                    + Math.Abs(isTrader ? 1f : 0f) * typed.ImportanceWeightTrader;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "pawnkind:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static string ResolveFactionLabel(PawnKindDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "defaultFactionType", out object factionType) && factionType is FactionDef byType)
            {
                return ProcessDefUtility.TrimOrNull(byType.label) ?? byType.defName ?? string.Empty;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "defaultFactionDef", out object factionDef) && factionDef is FactionDef byDef)
            {
                return ProcessDefUtility.TrimOrNull(byDef.label) ?? byDef.defName ?? string.Empty;
            }

            if (ProcessDefUtility.TryGetMemberValue(def, "faction", out object faction) && faction is FactionDef direct)
            {
                return ProcessDefUtility.TrimOrNull(direct.label) ?? direct.defName ?? string.Empty;
            }

            return string.Empty;
        }

        private static string ResolveCombatPowerLevel(float combatPower)
        {
            if (combatPower < 90f)
            {
                return "RimTalkGenKnowledge.Text.PawnKind.CombatPowerLevel.Low".Translate();
            }

            if (combatPower > 300f)
            {
                return "RimTalkGenKnowledge.Text.PawnKind.CombatPowerLevel.High".Translate();
            }

            return string.Empty;
        }
    }
}
