using System.Collections.Generic;
using Verse;

namespace GenKnowledge
{
    public class GeneratedKnowledgeItem
    {
        public string LogicalKey;
        public string Tag;
        public string Content;
        public float Importance;
    }

    public class ProcessDefContext
    {
    }

    public class GenerationReport
    {
        public int InputCount;
        public int CreatedCount;
        public int UpdatedCount;
        public int DeletedCount;
        public int FailedCount;
        public int SkippedCount;
        public int FinishedAtTick;
        public string LastError;
        public readonly List<string> Errors = new List<string>();

        public string BuildSummaryLine()
        {
            return "RimTalkGenKnowledge.Report.Summary".Translate(
                InputCount,
                CreatedCount,
                UpdatedCount,
                DeletedCount,
                SkippedCount,
                FailedCount);
        }

        public void AddError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            LastError = error;
            Errors.Add(error);
            FailedCount++;
        }
    }
}
