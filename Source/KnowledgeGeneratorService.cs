using System;
using System.Collections.Generic;
using System.Linq;
using GenKnowledge.ProcessDefs;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge
{
    public class KnowledgeGeneratorService
    {
        private readonly KnowledgeApiBridge apiBridge;
        private readonly List<IProcessDef> processors;
        private readonly Dictionary<string, ProcessDefBaseConfig> processConfigs;
        private readonly float minKnowledgeImportance;

        public KnowledgeGeneratorService(
            KnowledgeApiBridge apiBridge,
            List<IProcessDef> processors,
            Dictionary<string, ProcessDefBaseConfig> processConfigs,
            float minKnowledgeImportance)
        {
            this.apiBridge = apiBridge;
            this.processors = processors ?? new List<IProcessDef>();
            this.processConfigs = processConfigs;
            this.minKnowledgeImportance = Mathf.Clamp01(minKnowledgeImportance);
        }

        public GenerationReport Run(Dictionary<string, string> logicalToKnowledgeId, bool reportEachError)
        {
            var report = new GenerationReport
            {
                FinishedAtTick = Find.TickManager?.TicksGame ?? 0
            };

            if (Current.Game == null || Find.World == null)
            {
                AppendError(report, "RimTalkGenKnowledge.Message.GenerationOnlyInLoadedSave".Translate(), reportEachError);
                return report;
            }

            if (!apiBridge.Initialize())
            {
                AppendError(report, apiBridge.LastInitError ?? "RimTalkGenKnowledge.Message.ApiInitializationFailed".Translate(), reportEachError);
                return report;
            }

            if (logicalToKnowledgeId == null)
            {
                AppendError(report, "RimTalkGenKnowledge.Message.KnowledgeMappingNull".Translate(), reportEachError);
                return report;
            }

            var context = new ProcessDefContext();
            var generated = new List<GeneratedKnowledgeItem>();

            foreach (IProcessDef processor in processors)
            {
                try
                {
                    if (processor == null)
                    {
                        continue;
                    }

                    ProcessDefBaseConfig config = ResolveConfig(processor, report, reportEachError);
                    if (config == null || !config.Enabled)
                    {
                        continue;
                    }

                    IEnumerable<GeneratedKnowledgeItem> items = processor.ProcessDefs(context, config);
                    if (items != null)
                    {
                        generated.AddRange(items);
                    }
                }
                catch (Exception ex)
                {
                    AppendError(report, "RimTalkGenKnowledge.Message.ProcessorFailed".Translate(processor?.Id ?? processor?.GetType().Name, ex.Message), reportEachError);
                }
            }

            var validItems = generated
                .Where(item => IsValidItem(item, minKnowledgeImportance))
                .GroupBy(i => i.LogicalKey)
                .Select(g => g.Last())
                .ToList();

            report.InputCount = validItems.Count;

            var liveKeys = new HashSet<string>(validItems.Select(i => i.LogicalKey), StringComparer.Ordinal);
            var staleKeys = logicalToKnowledgeId.Keys.Where(key => !liveKeys.Contains(key)).ToList();

            foreach (string staleKey in staleKeys)
            {
                string id = logicalToKnowledgeId[staleKey];
                if (!string.IsNullOrWhiteSpace(id))
                {
                    bool removed = apiBridge.RemoveKnowledge(id, report);
                    if (removed)
                    {
                        report.DeletedCount++;
                    }
                    else
                    {
                        report.SkippedCount++;
                    }
                }

                logicalToKnowledgeId.Remove(staleKey);
            }

            foreach (GeneratedKnowledgeItem item in validItems)
            {
                try
                {
                    item.Importance = Mathf.Clamp01(item.Importance);

                    if (logicalToKnowledgeId.TryGetValue(item.LogicalKey, out string existingId) &&
                        !string.IsNullOrWhiteSpace(existingId) &&
                        apiBridge.ExistsKnowledge(existingId, report))
                    {
                        bool updated = apiBridge.UpdateKnowledge(existingId, item.Content, report);
                        if (updated)
                        {
                            report.UpdatedCount++;
                        }
                        else
                        {
                            report.SkippedCount++;
                        }

                        continue;
                    }

                    string createdId = apiBridge.AddKnowledge(item.Tag, item.Content, item.Importance, report);
                    if (!string.IsNullOrWhiteSpace(createdId))
                    {
                        logicalToKnowledgeId[item.LogicalKey] = createdId;
                        report.CreatedCount++;
                    }
                    else
                    {
                        report.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    AppendError(report, "RimTalkGenKnowledge.Message.UnhandledItemFailure".Translate(item.LogicalKey, ex.Message), reportEachError);
                }
            }

            return report;
        }

        public GenerationReport Clear(Dictionary<string, string> logicalToKnowledgeId, bool reportEachError)
        {
            var report = new GenerationReport
            {
                FinishedAtTick = Find.TickManager?.TicksGame ?? 0
            };

            if (Current.Game == null || Find.World == null)
            {
                AppendError(report, "RimTalkGenKnowledge.Message.ClearOnlyInLoadedSave".Translate(), reportEachError);
                return report;
            }

            if (!apiBridge.Initialize())
            {
                AppendError(report, apiBridge.LastInitError ?? "RimTalkGenKnowledge.Message.ApiInitializationFailed".Translate(), reportEachError);
                return report;
            }

            if (logicalToKnowledgeId == null)
            {
                AppendError(report, "RimTalkGenKnowledge.Message.KnowledgeMappingNull".Translate(), reportEachError);
                return report;
            }

            List<string> keys = logicalToKnowledgeId.Keys.ToList();
            foreach (string key in keys)
            {
                string id = logicalToKnowledgeId[key];
                if (string.IsNullOrWhiteSpace(id))
                {
                    logicalToKnowledgeId.Remove(key);
                    report.SkippedCount++;
                    continue;
                }

                bool removed = apiBridge.RemoveKnowledge(id, report);
                if (removed)
                {
                    report.DeletedCount++;
                }
                else
                {
                    report.SkippedCount++;
                }

                logicalToKnowledgeId.Remove(key);
            }

            return report;
        }

        private ProcessDefBaseConfig ResolveConfig(IProcessDef processor, GenerationReport report, bool reportEachError)
        {
            ProcessDefBaseConfig defaultConfig = processor.CreateDefaultConfig();
            if (defaultConfig == null)
            {
                AppendError(report, "RimTalkGenKnowledge.Message.ProcessorDefaultConfigNull".Translate(processor.Id), reportEachError);
                return null;
            }

            if (processConfigs == null || !processConfigs.TryGetValue(processor.Id, out ProcessDefBaseConfig config) || config == null)
            {
                return defaultConfig;
            }

            Type expectedType = defaultConfig.GetType();
            if (!expectedType.IsInstanceOfType(config))
            {
                AppendError(
                    report,
                    "RimTalkGenKnowledge.Message.ProcessorConfigTypeMismatch".Translate(processor.Id, expectedType.Name, config.GetType().Name),
                    reportEachError);
                return defaultConfig;
            }

            return config;
        }

        private static bool IsValidItem(GeneratedKnowledgeItem item, float minImportance)
        {
            if (item == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.LogicalKey) ||
                string.IsNullOrWhiteSpace(item.Tag) ||
                string.IsNullOrWhiteSpace(item.Content))
            {
                return false;
            }

            if (item.Tag.Length > 120 || item.Content.Length > 2000)
            {
                return false;
            }

            if (item.Importance < minImportance)
            {
                return false;
            }

            return true;
        }

        private static void AppendError(GenerationReport report, string error, bool reportEachError)
        {
            report.AddError(error);
            Log.Error($"[GenKnowledge] {error}");

            if (reportEachError)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenKnowledgeError".Translate(error), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
