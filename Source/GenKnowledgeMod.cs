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
            return "Gen Knowledge";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<IProcessDef> processors = ProcessDefRegistry.CreateProcessors();
            Settings.EnsureDefaults(processors);

            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("Enable global error reporting", ref Settings.enableGlobalErrorReporting);
            listing.GapLine();
            listing.Label("Generate knowledge only works in a loaded save.");
            listing.Gap();

            if (listing.ButtonText("Generate knowledge (current save)"))
            {
                RunGeneration();
            }

            if (listing.ButtonText("Clear generated knowledge"))
            {
                ClearGeneratedKnowledge();
            }

            listing.GapLine();
            listing.Label("Processors");

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
                Messages.Message("Generation is only available in a loaded save.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                Messages.Message("Generation failed: game component missing.", MessageTypeDefOf.RejectInput, false);
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
                Messages.Message("Clear is only available in a loaded save.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                Messages.Message("Clear failed: game component missing.", MessageTypeDefOf.RejectInput, false);
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
                listing.Label("No save loaded.");
                return;
            }

            var comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                listing.Label("No generation report available.");
                return;
            }

            GenerationReport report = comp.LastReport;
            if (report == null)
            {
                listing.Label("No generation report yet.");
                return;
            }

            listing.Label($"Last run tick: {report.FinishedAtTick}");
            listing.Label(report.BuildSummaryLine());

            if (!string.IsNullOrWhiteSpace(report.LastError))
            {
                listing.Label($"Last error: {report.LastError}");
            }
        }
    }
}
