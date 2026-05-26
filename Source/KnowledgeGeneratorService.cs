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
        private readonly bool debugIncludeInternalKeys;
        private readonly bool enableRealWorldSkipList;
        private readonly bool enableHighRedundancySkipList;

        public KnowledgeGeneratorService(
            KnowledgeApiBridge apiBridge,
            List<IProcessDef> processors,
            Dictionary<string, ProcessDefBaseConfig> processConfigs,
            float minKnowledgeImportance,
            bool debugIncludeInternalKeys,
            bool enableRealWorldSkipList,
            bool enableHighRedundancySkipList)
        {
            this.apiBridge = apiBridge;
            this.processors = processors ?? new List<IProcessDef>();
            this.processConfigs = processConfigs;
            this.minKnowledgeImportance = Mathf.Clamp01(minKnowledgeImportance);
            this.debugIncludeInternalKeys = debugIncludeInternalKeys;
            this.enableRealWorldSkipList = enableRealWorldSkipList;
            this.enableHighRedundancySkipList = enableHighRedundancySkipList;
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
            KnowledgeSkipRuleSet skipRules = BuildSkipRuleSet(report, reportEachError);

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

            var validCandidates = generated
                .Select(NormalizeItemForStorage)
                .Where(item => IsValidItem(item, minKnowledgeImportance))
                .ToList();

            if (skipRules != null && skipRules.ApproxRuleCount > 0)
            {
                int skippedByList = validCandidates.RemoveAll(item => skipRules.ShouldSkip(item));
                if (skippedByList > 0)
                {
                    report.SkippedCount += skippedByList;
                }
            }

            var validItems = validCandidates
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
                    if (debugIncludeInternalKeys)
                    {
                        item.Content = AttachDebugInternalKeyInfo(item);
                    }

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

        private KnowledgeSkipRuleSet BuildSkipRuleSet(GenerationReport report, bool reportEachError)
        {
            var merged = new KnowledgeSkipRuleSet();
            if (!enableRealWorldSkipList && !enableHighRedundancySkipList)
            {
                return merged;
            }

            if (enableRealWorldSkipList)
            {
                KnowledgeSkipRuleSet one = KnowledgeSkipListLoader.LoadRulesForRelativePath(
                    KnowledgeSkipListLoader.RealWorldListRelativePath,
                    out string error,
                    out string loadedPath);
                merged.MergeFrom(one);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendError(report, $"Knowledge skip list load issue ({loadedPath ?? KnowledgeSkipListLoader.RealWorldListRelativePath}): {error}", reportEachError);
                }
            }

            if (enableHighRedundancySkipList)
            {
                KnowledgeSkipRuleSet one = KnowledgeSkipListLoader.LoadRulesForRelativePath(
                    KnowledgeSkipListLoader.HighRedundancyListRelativePath,
                    out string error,
                    out string loadedPath);
                merged.MergeFrom(one);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendError(report, $"Knowledge skip list load issue ({loadedPath ?? KnowledgeSkipListLoader.HighRedundancyListRelativePath}): {error}", reportEachError);
                }
            }

            return merged;
        }

        private static GeneratedKnowledgeItem NormalizeItemForStorage(GeneratedKnowledgeItem item)
        {
            if (item == null)
            {
                return null;
            }

            item.Tag = NormalizeInlineText(item.Tag);
            item.Content = NormalizeInlineText(item.Content);
            return item;
        }

        private static string NormalizeInlineText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (normalized.IndexOf('\n') < 0)
            {
                return normalized.Trim();
            }

            string[] parts = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return string.Join("；", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
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

        private static string AttachDebugInternalKeyInfo(GeneratedKnowledgeItem item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string logicalKey = item.LogicalKey ?? string.Empty;
            ParseLogicalKey(logicalKey, out string keyPrefix, out string defName);
            string processorId = ResolveProcessorId(keyPrefix);
            string modPackageId = ResolveModPackageId(keyPrefix, defName);
            string debugPrefix = $"[debug|processorId={processorId}|logicalKey={logicalKey}|defName={defName}|modPackageId={modPackageId}]";

            string content = item.Content ?? string.Empty;
            if (content.StartsWith("[debug|", StringComparison.Ordinal))
            {
                int closing = content.IndexOf(']');
                if (closing >= 0 && closing + 1 < content.Length)
                {
                    content = content.Substring(closing + 1);
                }
                else
                {
                    content = string.Empty;
                }
            }

            return debugPrefix + content;
        }

        private static void ParseLogicalKey(string logicalKey, out string keyPrefix, out string defName)
        {
            keyPrefix = string.Empty;
            defName = string.Empty;
            if (string.IsNullOrWhiteSpace(logicalKey))
            {
                return;
            }

            int firstSep = logicalKey.IndexOf(':');
            if (firstSep <= 0)
            {
                keyPrefix = logicalKey;
                return;
            }

            keyPrefix = logicalKey.Substring(0, firstSep);
            string rest = firstSep + 1 < logicalKey.Length ? logicalKey.Substring(firstSep + 1) : string.Empty;
            if (string.Equals(keyPrefix, "trait", StringComparison.OrdinalIgnoreCase))
            {
                int secondSep = rest.IndexOf(':');
                defName = secondSep > 0 ? rest.Substring(0, secondSep) : rest;
                return;
            }

            defName = rest;
        }

        private static string ResolveProcessorId(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return "UnknownProcessor";
            }

            if (string.Equals(keyPrefix, "xenotype", StringComparison.OrdinalIgnoreCase)) return XenotypeDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "thing", StringComparison.OrdinalIgnoreCase)) return ThingDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "pawnkind", StringComparison.OrdinalIgnoreCase)) return PawnKindDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "trait", StringComparison.OrdinalIgnoreCase)) return TraitDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "research", StringComparison.OrdinalIgnoreCase)) return ResearchProjectDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "recipe", StringComparison.OrdinalIgnoreCase)) return RecipeDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "hediff", StringComparison.OrdinalIgnoreCase)) return HediffDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "gene", StringComparison.OrdinalIgnoreCase)) return GeneDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "meme", StringComparison.OrdinalIgnoreCase)) return MemeDefProcessor.ProcessorId;
            if (string.Equals(keyPrefix, "faction", StringComparison.OrdinalIgnoreCase)) return FactionDefProcessor.ProcessorId;

            return keyPrefix;
        }

        private static string ResolveModPackageId(string keyPrefix, string defName)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix) || string.IsNullOrWhiteSpace(defName))
            {
                return "unknown";
            }

            Def def = null;

            if (string.Equals(keyPrefix, "xenotype", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<XenotypeDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "thing", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "pawnkind", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "trait", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "research", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "recipe", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "hediff", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "gene", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<GeneDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "meme", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<MemeDef>.GetNamedSilentFail(defName);
            }
            else if (string.Equals(keyPrefix, "faction", StringComparison.OrdinalIgnoreCase))
            {
                def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
            }

            if (def == null || def.modContentPack == null)
            {
                return "unknown";
            }

            string packageId = ProcessDefUtility.ReadPackageId(def.modContentPack);
            return string.IsNullOrWhiteSpace(packageId) ? "unknown" : packageId;
        }
    }
}
