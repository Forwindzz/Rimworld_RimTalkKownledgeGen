using System.Collections.Generic;
using GenKnowledge.ProcessDefs;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge
{
    public class GenKnowledgeMod : Mod
    {
        public static GenKnowledgeSettings Settings;
        private Vector2 settingsScrollPosition = Vector2.zero;

        public GenKnowledgeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GenKnowledgeSettings>();
            Settings.EnsureDefaults(ProcessDefRegistry.CreateProcessors());
        }

        public override string SettingsCategory()
        {
            return "RimTalkGenKnowledge.Settings.Category".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<IProcessDef> processors = ProcessDefRegistry.CreateProcessors();
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

                estimatedHeight += processor.GetConfigHeight(cfg, inRect.width - 28f) + 48f;
            }

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Mathf.Max(inRect.height, estimatedHeight));
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.EnableGlobalErrorReporting".Translate(), ref Settings.enableGlobalErrorReporting);
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

                listing.Label(processor.DisplayName);
                float configHeight = processor.GetConfigHeight(config, viewRect.width);
                Rect container = listing.GetRect(configHeight);
                Widgets.DrawMenuSection(container);
                Rect contentRect = container.ContractedBy(6f);
                processor.DrawConfig(contentRect, config);
                listing.Gap(6f);
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

        private static void ResetAllProcessConfigs(List<IProcessDef> processors)
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
    }
}
