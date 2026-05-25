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

        public KnowledgeGeneratorService(
            KnowledgeApiBridge apiBridge,
            List<IProcessDef> processors,
            Dictionary<string, ProcessDefBaseConfig> processConfigs)
        {
            this.apiBridge = apiBridge;
            this.processors = processors ?? new List<IProcessDef>();
            this.processConfigs = processConfigs;
        }

        public GenerationReport Run(Dictionary<string, string> logicalToKnowledgeId, bool reportEachError)
        {
            var report = new GenerationReport
            {
                FinishedAtTick = Find.TickManager?.TicksGame ?? 0
            };

            if (Current.Game == null || Find.World == null)
            {
                AppendError(report, "Generation is only available in a loaded save.", reportEachError);
                return report;
            }

            if (!ModsConfig.BiotechActive)
            {
                AppendError(report, "Biotech is not active. XenotypeDef generation skipped.", reportEachError);
                return report;
            }

            if (!apiBridge.Initialize())
            {
                AppendError(report, apiBridge.LastInitError ?? "API initialization failed.", reportEachError);
                return report;
            }

            if (logicalToKnowledgeId == null)
            {
                AppendError(report, "Knowledge mapping dictionary is null.", reportEachError);
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
                    AppendError(report, $"Processor {processor?.Id ?? processor?.GetType().Name} failed: {ex.Message}", reportEachError);
                }
            }

            var validItems = generated
                .Where(IsValidItem)
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
                    AppendError(report, $"Unhandled item failure ({item.LogicalKey}): {ex.Message}", reportEachError);
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
                AppendError(report, "Clear is only available in a loaded save.", reportEachError);
                return report;
            }

            if (!apiBridge.Initialize())
            {
                AppendError(report, apiBridge.LastInitError ?? "API initialization failed.", reportEachError);
                return report;
            }

            if (logicalToKnowledgeId == null)
            {
                AppendError(report, "Knowledge mapping dictionary is null.", reportEachError);
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
                AppendError(report, $"Processor {processor.Id} returned null default config.", reportEachError);
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
                    $"Processor {processor.Id} config type mismatch. Expected {expectedType.Name}, got {config.GetType().Name}.",
                    reportEachError);
                return defaultConfig;
            }

            return config;
        }

        private static bool IsValidItem(GeneratedKnowledgeItem item)
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

            return true;
        }

        private static void AppendError(GenerationReport report, string error, bool reportEachError)
        {
            report.AddError(error);
            Log.Error($"[GenKnowledge] {error}");

            if (reportEachError)
            {
                Messages.Message($"GenKnowledge error: {error}", MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
