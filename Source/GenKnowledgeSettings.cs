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

                if (!processConfigs.TryGetValue(processor.Id, out ProcessDefBaseConfig config) || config == null)
                {
                    processConfigs[processor.Id] = processor.CreateDefaultConfig();
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

            if (!processConfigs.TryGetValue(processor.Id, out ProcessDefBaseConfig config) || config == null)
            {
                config = processor.CreateDefaultConfig();
                processConfigs[processor.Id] = config;
            }

            return config;
        }
    }
}
