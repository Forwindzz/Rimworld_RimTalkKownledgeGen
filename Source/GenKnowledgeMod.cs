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

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("RimTalkGenKnowledge.Settings.EnableGlobalErrorReporting".Translate(), ref Settings.enableGlobalErrorReporting);
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
                float configHeight = processor.GetConfigHeight(config, inRect.width);
                Rect container = listing.GetRect(configHeight);
                Widgets.DrawMenuSection(container);
                Rect contentRect = container.ContractedBy(6f);
                processor.DrawConfig(contentRect, config);
                listing.Gap(6f);
            }

            listing.GapLine();
            DrawLastReport(listing);

            listing.End();

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
    }
}
