using System.Collections.Generic;
using GenKnowledge.ProcessDefs;
using Verse;

namespace GenKnowledge
{
    public class GenKnowledgeGameComponent : GameComponent
    {
        private Dictionary<string, string> logicalToKnowledgeId = new Dictionary<string, string>();
        private GenerationReport lastReport;

        public GenerationReport LastReport => lastReport;

        public GenKnowledgeGameComponent(Game game)
        {
        }

        public GenerationReport RunGeneration(bool reportEachError)
        {
            if (logicalToKnowledgeId == null)
            {
                logicalToKnowledgeId = new Dictionary<string, string>();
            }

            var processors = ProcessDefRegistry.CreateProcessors();
            GenKnowledgeMod.Settings?.EnsureDefaults(processors);

            var service = new KnowledgeGeneratorService(
                new KnowledgeApiBridge(reportEachError),
                processors,
                GenKnowledgeMod.Settings?.processConfigs,
                GenKnowledgeMod.Settings?.minKnowledgeImportance ?? 0.21f,
                GenKnowledgeMod.Settings?.debugIncludeInternalKeys ?? false,
                GenKnowledgeMod.Settings?.enableRealWorldSkipList ?? true,
                GenKnowledgeMod.Settings?.enableHighRedundancySkipList ?? false);

            lastReport = service.Run(logicalToKnowledgeId, reportEachError);
            return lastReport;
        }

        public GenerationReport ClearGeneratedKnowledge(bool reportEachError)
        {
            if (logicalToKnowledgeId == null)
            {
                logicalToKnowledgeId = new Dictionary<string, string>();
            }

            var service = new KnowledgeGeneratorService(
                new KnowledgeApiBridge(reportEachError),
                ProcessDefRegistry.CreateProcessors(),
                GenKnowledgeMod.Settings?.processConfigs,
                GenKnowledgeMod.Settings?.minKnowledgeImportance ?? 0.21f,
                GenKnowledgeMod.Settings?.debugIncludeInternalKeys ?? false,
                GenKnowledgeMod.Settings?.enableRealWorldSkipList ?? true,
                GenKnowledgeMod.Settings?.enableHighRedundancySkipList ?? false);

            lastReport = service.Clear(logicalToKnowledgeId, reportEachError);
            return lastReport;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref logicalToKnowledgeId, "logicalToKnowledgeId", LookMode.Value, LookMode.Value);

            int finishedAtTick = lastReport?.FinishedAtTick ?? 0;
            int inputCount = lastReport?.InputCount ?? 0;
            int createdCount = lastReport?.CreatedCount ?? 0;
            int updatedCount = lastReport?.UpdatedCount ?? 0;
            int deletedCount = lastReport?.DeletedCount ?? 0;
            int failedCount = lastReport?.FailedCount ?? 0;
            int skippedCount = lastReport?.SkippedCount ?? 0;
            string lastError = lastReport?.LastError;

            Scribe_Values.Look(ref finishedAtTick, "reportFinishedAtTick", 0);
            Scribe_Values.Look(ref inputCount, "reportInputCount", 0);
            Scribe_Values.Look(ref createdCount, "reportCreatedCount", 0);
            Scribe_Values.Look(ref updatedCount, "reportUpdatedCount", 0);
            Scribe_Values.Look(ref deletedCount, "reportDeletedCount", 0);
            Scribe_Values.Look(ref failedCount, "reportFailedCount", 0);
            Scribe_Values.Look(ref skippedCount, "reportSkippedCount", 0);
            Scribe_Values.Look(ref lastError, "reportLastError");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (logicalToKnowledgeId == null)
                {
                    logicalToKnowledgeId = new Dictionary<string, string>();
                }
                if (finishedAtTick > 0 || inputCount > 0 || createdCount > 0 || updatedCount > 0 || deletedCount > 0 || failedCount > 0 || skippedCount > 0)
                {
                    lastReport = new GenerationReport
                    {
                        FinishedAtTick = finishedAtTick,
                        InputCount = inputCount,
                        CreatedCount = createdCount,
                        UpdatedCount = updatedCount,
                        DeletedCount = deletedCount,
                        FailedCount = failedCount,
                        SkippedCount = skippedCount,
                        LastError = lastError
                    };
                }
                else
                {
                    lastReport = null;
                }
            }
        }
    }
}
