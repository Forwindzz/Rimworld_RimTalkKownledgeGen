using System.Collections.Generic;
using System.Linq;
using GenKnowledge.ProcessDefs;
using UnityEngine;
using Verse;

namespace GenKnowledge
{
    public class GenKnowledgeSettings : ModSettings
    {
        public bool enableGlobalErrorReporting = false;
        public bool debugIncludeInternalKeys = false;
        public bool showNumericValues = false;
        public bool enableMemoryUiPatch = true;
        public bool enableRealWorldSkipList = true;
        public bool enableHighRedundancySkipList = false;
        public float minKnowledgeImportance = 0.21f;

        public Dictionary<string, ProcessDefBaseConfig> processConfigs = new Dictionary<string, ProcessDefBaseConfig>();

        private List<string> processConfigKeysWorkingList;
        private List<ProcessDefBaseConfig> processConfigValuesWorkingList;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableGlobalErrorReporting, "enableGlobalErrorReporting", false);
            Scribe_Values.Look(ref debugIncludeInternalKeys, "debugIncludeInternalKeys", false);
            Scribe_Values.Look(ref showNumericValues, "showNumericValues", false);
            Scribe_Values.Look(ref enableMemoryUiPatch, "enableMemoryUiPatch", true);
            Scribe_Values.Look(ref enableRealWorldSkipList, "enableRealWorldSkipList", true);
            Scribe_Values.Look(ref enableHighRedundancySkipList, "enableHighRedundancySkipList", false);
            Scribe_Values.Look(ref minKnowledgeImportance, "minKnowledgeImportance", 0.21f);
            Scribe_Collections.Look(
                ref processConfigs,
                "processConfigs",
                LookMode.Value,
                LookMode.Deep,
                ref processConfigKeysWorkingList,
                ref processConfigValuesWorkingList);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                minKnowledgeImportance = Mathf.Clamp01(minKnowledgeImportance);

                if (processConfigs == null)
                {
                    processConfigs = new Dictionary<string, ProcessDefBaseConfig>();
                }

                // Remove invalid keys loaded from old or broken data.
                List<string> invalidKeys = processConfigs.Keys.Where(string.IsNullOrWhiteSpace).ToList();
                foreach (string key in invalidKeys)
                {
                    processConfigs.Remove(key);
                }
            }
        }

        public void EnsureDefaults(IEnumerable<IProcessDef> processors)
        {
            if (processConfigs == null)
            {
                processConfigs = new Dictionary<string, ProcessDefBaseConfig>();
            }

            if (processors == null)
            {
                return;
            }

            foreach (IProcessDef processor in processors)
            {
                if (processor == null || string.IsNullOrWhiteSpace(processor.Id))
                {
                    continue;
                }

                ProcessDefBaseConfig defaultConfig = processor.CreateDefaultConfig();
                if (defaultConfig == null)
                {
                    continue;
                }

                if (!processConfigs.TryGetValue(processor.Id, out ProcessDefBaseConfig config)
                    || config == null
                    || !defaultConfig.GetType().IsInstanceOfType(config))
                {
                    processConfigs[processor.Id] = defaultConfig;
                }
            }
        }

        public ProcessDefBaseConfig GetOrCreateConfig(IProcessDef processor)
        {
            if (processor == null || string.IsNullOrWhiteSpace(processor.Id))
            {
                return null;
            }

            if (processConfigs == null)
            {
                processConfigs = new Dictionary<string, ProcessDefBaseConfig>();
            }

            ProcessDefBaseConfig defaultConfig = processor.CreateDefaultConfig();
            if (defaultConfig == null)
            {
                return null;
            }

            if (!processConfigs.TryGetValue(processor.Id, out ProcessDefBaseConfig config)
                || config == null
                || !defaultConfig.GetType().IsInstanceOfType(config))
            {
                config = defaultConfig;
                processConfigs[processor.Id] = config;
            }

            return config;
        }
    }
}
