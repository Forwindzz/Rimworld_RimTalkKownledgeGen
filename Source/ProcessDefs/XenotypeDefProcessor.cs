using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class XenotypeDefProcessor : ProcessDefProcessorBase<XenotypeProcessDefConfig>
    {
        public const string ProcessorId = "XenotypeDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Xenotype.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new XenotypeProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Xenotype.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Xenotype.Knowledge".Translate(),
                BaseImportance = 0.75f,
                ImportanceMin = 0f,
                ImportanceMax = 1f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            XenotypeProcessDefConfig defaults = (XenotypeProcessDefConfig)CreateDefaultConfig();
            CopyBaseConfigFields(defaults, typed);
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Xenotype label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Xenotype description" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            XenotypeProcessDefConfig typed = config as XenotypeProcessDefConfig ?? (XenotypeProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled || !ModsConfig.BiotechActive)
            {
                yield break;
            }

            List<XenotypeDef> defs = DefDatabase<XenotypeDef>.AllDefsListForReading;
            if (defs == null)
            {
                yield break;
            }

            foreach (XenotypeDef def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

                if (!ProcessDefUtility.ShouldIncludeDef(def, typed.IncludeModDefs))
                {
                    continue;
                }

                string label = ProcessDefUtility.TrimOrNull(def.label);
                string description = ProcessDefUtility.TrimOrNull(def.description);
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float importance = ComputeFinalImportance(typed.BaseImportance, typed);
                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "xenotype:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = importance
                };
            }
        }
    }
}
