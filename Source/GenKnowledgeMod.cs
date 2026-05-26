using System;
using System.Collections.Generic;
using GenKnowledge.Compatibility;
using GenKnowledge.ProcessDefs;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge
{
    public class GenKnowledgeMod : Mod
    {
        public static GenKnowledgeSettings Settings;
        public static string ModRootDir { get; private set; }
        private Vector2 settingsScrollPosition = Vector2.zero;
        private readonly Dictionary<string, bool> processorFoldoutStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public GenKnowledgeMod(ModContentPack content) : base(content)
        {
            ModRootDir = content?.RootDir?.ToString();
            Settings = GetSettings<GenKnowledgeSettings>();
            Settings.EnsureDefaults(ProcessDefRegistry.GetProcessors());

            var harmony = new Harmony("RimTalk.GenKnowledge");
            harmony.PatchAll();
            MemoryAutoGenerateUiPatch.TryApply(harmony);
        }

        public override string SettingsCategory()
        {
            return "RimTalkGenKnowledge.Settings.Category".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            IReadOnlyList<IProcessDef> processors = ProcessDefRegistry.GetProcessors();
            Settings.EnsureDefaults(processors);

            float estimatedHeight = 220f;
            foreach (IProcessDef processor in processors)
            {
                if (processor == null)
                {
                    continue;
                }

                ProcessDefBaseConfig cfg = Settings.GetOrCreateConfig(processor);
                if (cfg == null)
                {
                    continue;
                }

                estimatedHeight += 34f;
                if (IsProcessorExpanded(processor.Id))
                {
                    estimatedHeight += processor.GetConfigHeight(cfg, inRect.width - 28f) + 12f;
                }
            }

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Mathf.Max(inRect.height, estimatedHeight));
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.EnableGlobalErrorReporting".Translate(), ref Settings.enableGlobalErrorReporting);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.DebugIncludeInternalKeys".Translate(), ref Settings.debugIncludeInternalKeys);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.ShowNumericValuesGlobal".Translate(), ref Settings.showNumericValues);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.EnableMemoryUiPatch".Translate(), ref Settings.enableMemoryUiPatch);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.EnableGlobalLabelDedup".Translate(), ref Settings.enableGlobalLabelDedup);
            listing.Label("RimTalkGenKnowledge.Settings.LabelDedupSimilarityThreshold".Translate(Settings.labelDedupSimilarityThreshold.ToString("0.00")));
            Settings.labelDedupSimilarityThreshold = Mathf.Clamp01(listing.Slider(Settings.labelDedupSimilarityThreshold, 0f, 1f));
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.LabelDedupHighSimilarityKeepLongest".Translate(), ref Settings.labelDedupHighSimilarityKeepLongest);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.LabelDedupLowSimilarityMerge".Translate(), ref Settings.labelDedupLowSimilarityMerge);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.SkipList.RealWorld".Translate(), ref Settings.enableRealWorldSkipList);
            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.SkipList.HighRedundancy".Translate(), ref Settings.enableHighRedundancySkipList);
            listing.Label("RimTalkGenKnowledge.Settings.MinKnowledgeImportance".Translate(Settings.minKnowledgeImportance.ToString("0.00")));
            Settings.minKnowledgeImportance = Mathf.Clamp01(listing.Slider(Settings.minKnowledgeImportance, 0f, 1f));
            listing.GapLine();
            listing.Label("RimTalkGenKnowledge.Settings.OnlyLoadedSave".Translate());
            listing.Gap();

            if (listing.ButtonText("RimTalkGenKnowledge.Settings.GenerateCurrentSave".Translate()))
            {
                RunGeneration();
            }

            if (listing.ButtonText("RimTalkGenKnowledge.Settings.ClearGeneratedKnowledge".Translate()))
            {
                ClearGeneratedKnowledge();
            }

            if (listing.ButtonText("RimTalkGenKnowledge.Settings.ResetAllProcessConfigs".Translate()))
            {
                ResetAllProcessConfigs(processors);
            }

            listing.GapLine();
            listing.Label("RimTalkGenKnowledge.Settings.Processors".Translate());

            foreach (IProcessDef processor in processors)
            {
                if (processor == null)
                {
                    continue;
                }

                ProcessDefBaseConfig config = Settings.GetOrCreateConfig(processor);
                if (config == null)
                {
                    continue;
                }

                bool expanded = IsProcessorExpanded(processor.Id);
                Rect foldoutRect = listing.GetRect(30f);
                string foldoutText = (expanded ? "[-] " : "[+] ") + processor.DisplayName;
                if (Widgets.ButtonText(foldoutRect, foldoutText))
                {
                    expanded = !expanded;
                    SetProcessorExpanded(processor.Id, expanded);
                }

                if (expanded)
                {
                    float configHeight = processor.GetConfigHeight(config, viewRect.width);
                    Rect container = listing.GetRect(configHeight);
                    Widgets.DrawMenuSection(container);
                    Rect contentRect = container.ContractedBy(6f);
                    processor.DrawConfig(contentRect, config);
                    listing.Gap(6f);
                }
            }

            listing.GapLine();
            DrawLastReport(listing);

            listing.End();
            Widgets.EndScrollView();

            if (GUI.changed)
            {
                WriteSettings();
            }

            base.DoSettingsWindowContents(inRect);
        }

        private void RunGeneration()
        {
            if (Current.Game == null || Find.World == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenerationOnlyInLoadedSave".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenerationFailedMissingComponent".Translate(), MessageTypeDefOf.RejectInput, false);
                Log.Error("[GenKnowledge] Game component is missing.");
                return;
            }

            GenerationReport report = comp.RunGeneration(Settings.enableGlobalErrorReporting);
            MessageTypeDef messageType = report.FailedCount > 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.TaskCompletion;
            Messages.Message(report.BuildSummaryLine(), messageType, false);
        }

        private void ClearGeneratedKnowledge()
        {
            if (Current.Game == null || Find.World == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.ClearOnlyInLoadedSave".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.ClearFailedMissingComponent".Translate(), MessageTypeDefOf.RejectInput, false);
                Log.Error("[GenKnowledge] Game component is missing.");
                return;
            }

            GenerationReport report = comp.ClearGeneratedKnowledge(Settings.enableGlobalErrorReporting);
            MessageTypeDef messageType = report.FailedCount > 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.TaskCompletion;
            Messages.Message(report.BuildSummaryLine(), messageType, false);
        }

        private static void DrawLastReport(Listing_Standard listing)
        {
            if (Current.Game == null)
            {
                listing.Label("RimTalkGenKnowledge.Report.NoSaveLoaded".Translate());
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                listing.Label("RimTalkGenKnowledge.Report.NoGenerationReport".Translate());
                return;
            }

            GenerationReport report = comp.LastReport;
            if (report == null)
            {
                listing.Label("RimTalkGenKnowledge.Report.NoGenerationReportYet".Translate());
                return;
            }

            listing.Label("RimTalkGenKnowledge.Report.LastRunTick".Translate(report.FinishedAtTick));
            listing.Label(report.BuildSummaryLine());

            if (!string.IsNullOrWhiteSpace(report.LastError))
            {
                listing.Label("RimTalkGenKnowledge.Report.LastError".Translate(report.LastError));
            }
        }

        private static void ResetAllProcessConfigs(IEnumerable<IProcessDef> processors)
        {
            if (Settings == null)
            {
                return;
            }

            if (Settings.processConfigs == null)
            {
                Settings.processConfigs = new Dictionary<string, ProcessDefBaseConfig>();
            }
            else
            {
                Settings.processConfigs.Clear();
            }

            if (processors != null)
            {
                foreach (IProcessDef processor in processors)
                {
                    if (processor == null || string.IsNullOrWhiteSpace(processor.Id))
                    {
                        continue;
                    }

                    ProcessDefBaseConfig defaults = processor.CreateDefaultConfig();
                    if (defaults != null)
                    {
                        Settings.processConfigs[processor.Id] = defaults;
                    }
                }
            }

            Messages.Message("RimTalkGenKnowledge.Message.ResetAllProcessConfigsDone".Translate(), MessageTypeDefOf.TaskCompletion, false);
        }

        private bool IsProcessorExpanded(string processorId)
        {
            if (string.IsNullOrWhiteSpace(processorId))
            {
                return false;
            }

            if (!processorFoldoutStates.TryGetValue(processorId, out bool expanded))
            {
                expanded = false;
                processorFoldoutStates[processorId] = false;
            }
            return expanded;
        }

        private void SetProcessorExpanded(string processorId, bool expanded)
        {
            if (string.IsNullOrWhiteSpace(processorId))
            {
                return;
            }

            processorFoldoutStates[processorId] = expanded;
        }
    }
}

