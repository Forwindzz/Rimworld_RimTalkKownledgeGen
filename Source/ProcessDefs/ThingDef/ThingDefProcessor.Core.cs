using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public partial class ThingDefProcessor : ProcessDefProcessorBase<ThingProcessDefConfig>
    {
         const string KindBuilding = "Building";
        private const string KindFood = "Food";
        private const string KindMedicine = "Medicine";
        private const string KindApparel = "Apparel";
        private const string KindWeapon = "Weapon";
        private const string KindItem = "Item";

        private class PropertyObservation
        {
            public string PropertyKey;
            public float SignedC;
            public float StrengthD;
            public string TendencyText;
            public string DisplayLine;
        }

        private class SemanticLineEntry
        {
            public string PropertyKey;
            public string Text;
        }

        public const string ProcessorId = "ThingDefProcessor";
        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Thing.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            ThingProcessDefConfig config = new ThingProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Thing.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Thing.Knowledge".Translate(),
                BaseImportance = 0.1f,
                ImportanceMin = 0f,
                ImportanceMax = 0.8f,
                MaxDescriptionLength = 300,
                FilterDescriptionShorterThanLabel = true,
                DescriptionMinLabelLengthMultiplier = 3f,
                FilterFertilizedEggVariants = true,
                FertilizedEggVariantTokens = "RimTalkGenKnowledge.Text.Thing.Filter.FertilizedEggTokens".Translate(),
                ImportanceWeightMarketValueLog10 = 0.05f,
                ImportanceWeightMassLog10 = 0.001f,
                ImportanceWeightHitPointsLog10 = 0.02f,
                ImportanceWeightStackLimitIsOne = 0.05f,
                ImportanceWeightSpecialValueScore = 0.02f,
                ImportanceWeightNutrition = 0.05f,
                ImportanceWeightDescriptionLengthRatio = 0.02f,
                ImportanceMultiplierCraftable = 0.85f,
                EnableCategoryExtraText = true,
                EnablePriceFeelingText = true,
                EnableHitPointsFeelingText = true,
                DebugForceShowDeviation = false,
                FilterIntermediateBuildStates = true,
                IntermediateBuildStateTokens = "RimTalkGenKnowledge.Text.Thing.Filter.IntermediateStateTokens".Translate(),
                MaxSemanticLinesGlobal = 8,
                SpecialValueTopN = 3
            };
            EnsureDefaults(config);
            return config;
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            ThingProcessDefConfig typed = config as ThingProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            ThingProcessDefConfig defaults = (ThingProcessDefConfig)CreateDefaultConfig();
            CopyBaseConfigFields(defaults, typed);
            typed.MaxDescriptionLength = defaults.MaxDescriptionLength;
            typed.FilterDescriptionShorterThanLabel = defaults.FilterDescriptionShorterThanLabel;
            typed.DescriptionMinLabelLengthMultiplier = defaults.DescriptionMinLabelLengthMultiplier;
            typed.FilterFertilizedEggVariants = defaults.FilterFertilizedEggVariants;
            typed.FertilizedEggVariantTokens = defaults.FertilizedEggVariantTokens;
            typed.ImportanceWeightMarketValueLog10 = defaults.ImportanceWeightMarketValueLog10;
            typed.ImportanceWeightMassLog10 = defaults.ImportanceWeightMassLog10;
            typed.ImportanceWeightHitPointsLog10 = defaults.ImportanceWeightHitPointsLog10;
            typed.ImportanceWeightStackLimitIsOne = defaults.ImportanceWeightStackLimitIsOne;
            typed.ImportanceWeightSpecialValueScore = defaults.ImportanceWeightSpecialValueScore;
            typed.ImportanceWeightNutrition = defaults.ImportanceWeightNutrition;
            typed.ImportanceWeightDescriptionLengthRatio = defaults.ImportanceWeightDescriptionLengthRatio;
            typed.ImportanceMultiplierCraftable = defaults.ImportanceMultiplierCraftable;
            typed.EnableCategoryExtraText = defaults.EnableCategoryExtraText;
            typed.EnablePriceFeelingText = defaults.EnablePriceFeelingText;
            typed.EnableHitPointsFeelingText = defaults.EnableHitPointsFeelingText;
            typed.DebugForceShowDeviation = defaults.DebugForceShowDeviation;
            typed.FilterIntermediateBuildStates = defaults.FilterIntermediateBuildStates;
            typed.IntermediateBuildStateTokens = defaults.IntermediateBuildStateTokens;
            typed.MaxSemanticLinesGlobal = defaults.MaxSemanticLinesGlobal;
            typed.SpecialValueTopN = defaults.SpecialValueTopN;
            typed.CategoryRules = CloneCategoryRules(defaults.CategoryRules);
            typed.PropertyDeviationConfigs = ClonePropertyConfigs(defaults.PropertyDeviationConfigs);
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Thing label" },
                new PlaceholderDescriptor { Key = "labelDelimiter", Token = "{{labelDelimiter}}", Description = "Label/description delimiter" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Thing description" },
                new PlaceholderDescriptor { Key = "category", Token = "{{category}}", Description = "Thing category" },
                new PlaceholderDescriptor { Key = "categoryPrefix", Token = "{{categoryPrefix}}", Description = "Category line prefix" },
                new PlaceholderDescriptor { Key = "marketValue", Token = "{{marketValue}}", Description = "Market value number" },
                new PlaceholderDescriptor { Key = "mass", Token = "{{mass}}", Description = "Mass" },
                new PlaceholderDescriptor { Key = "stackLimit", Token = "{{stackLimit}}", Description = "Stack limit" },
                new PlaceholderDescriptor { Key = "techLevel", Token = "{{techLevel}}", Description = "Tech level" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" },
                new PlaceholderDescriptor { Key = "categoryText", Token = "{{categoryText}}", Description = "Category summary" },
                new PlaceholderDescriptor { Key = "techLevelText", Token = "{{techLevelText}}", Description = "Tech level text" },
                new PlaceholderDescriptor { Key = "modSource", Token = "{{modSource}}", Description = "Mod source" },
                new PlaceholderDescriptor { Key = "modSourceLine", Token = "{{modSourceLine}}", Description = "Full mod-source line, hidden when source is Core" },
                new PlaceholderDescriptor { Key = "marketValueText", Token = "{{marketValueText}}", Description = "Market value with tendency" },
                new PlaceholderDescriptor { Key = "hpText", Token = "{{hpText}}", Description = "HP with tendency" },
                new PlaceholderDescriptor { Key = "techLevelLine", Token = "{{techLevelLine}}", Description = "Full tech-level line, hidden when undefined" },
                new PlaceholderDescriptor { Key = "marketValueLine", Token = "{{marketValueLine}}", Description = "Full market-value line, only shown on obvious deviation" },
                new PlaceholderDescriptor { Key = "hpLine", Token = "{{hpLine}}", Description = "Full HP line, only shown on obvious deviation" },
                new PlaceholderDescriptor { Key = "categoryExtraText", Token = "{{categoryExtraText}}", Description = "Category semantic lines" },
                new PlaceholderDescriptor { Key = "thingCategories", Token = "{{thingCategories}}", Description = "Thing categories" },
                new PlaceholderDescriptor { Key = "tradeTags", Token = "{{tradeTags}}", Description = "Trade tags" },
                new PlaceholderDescriptor { Key = "weaponTags", Token = "{{weaponTags}}", Description = "Weapon tags" },
                new PlaceholderDescriptor { Key = "maxHitPoints", Token = "{{maxHitPoints}}", Description = "Max hit points value" }
            };
        }
    }
}

