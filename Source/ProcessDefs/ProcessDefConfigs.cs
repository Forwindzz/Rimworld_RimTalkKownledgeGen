using System.Collections.Generic;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public abstract class ProcessDefBaseConfig : IExposable
    {
        public bool Enabled = true;
        public bool IncludeModDefs = true;
        public string TagTemplate = "{{label}}";
        public string KnowledgeTemplate = "{{label}}: {{description}}";
        public float BaseImportance = 0.5f;
        public float ImportanceMin = 0f;
        public float ImportanceMax = 1f;

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref IncludeModDefs, "includeModDefs", true);
            Scribe_Values.Look(ref TagTemplate, "tagTemplate", "{{label}}");
            Scribe_Values.Look(ref KnowledgeTemplate, "knowledgeTemplate", "{{label}}: {{description}}");
            Scribe_Values.Look(ref BaseImportance, "baseImportance", 0.5f);
            Scribe_Values.Look(ref ImportanceMin, "importanceMin", 0f);
            Scribe_Values.Look(ref ImportanceMax, "importanceMax", 1f);
        }
    }

    public class XenotypeProcessDefConfig : ProcessDefBaseConfig
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }
    }

    public class ThingProcessDefConfig : ProcessDefBaseConfig
    {
        public int MaxDescriptionLength = 300;
        public bool FilterDescriptionShorterThanLabel = true;
        public float DescriptionMinLabelLengthMultiplier = 3f;
        public bool FilterFertilizedEggVariants = true;
        public string FertilizedEggVariantTokens = "fert.,unfert.,fertilized,unfertilized";

        public float ImportanceWeightMarketValueLog10 = 0.05f;
        public float ImportanceWeightMassLog10 = 0.001f;
        public float ImportanceWeightHitPointsLog10 = 0.02f;
        public float ImportanceWeightStackLimitIsOne = 0.05f;
        public float ImportanceWeightSpecialValueScore = 0.01f;
        public float ImportanceWeightNutrition = 0.05f;
        public float ImportanceWeightDescriptionLengthRatio = 0.02f;
        public float ImportanceMultiplierCraftable = 0.95f;

        public bool EnableCategoryExtraText = true;
        public bool EnablePriceFeelingText = true;
        public bool EnableHitPointsFeelingText = true;
        public bool DebugForceShowDeviation = false;
        public bool FilterIntermediateBuildStates = true;
        public string IntermediateBuildStateTokens = "blueprint,frame";

        public int MaxSemanticLinesGlobal = 8;
        public int SpecialValueTopN = 3;

        public Dictionary<string, ThingCategoryRuleConfig> CategoryRules = new Dictionary<string, ThingCategoryRuleConfig>();
        public Dictionary<string, ThingPropertyDeviationConfig> PropertyDeviationConfigs = new Dictionary<string, ThingPropertyDeviationConfig>();

        private List<string> categoryRuleKeysWorkingList;
        private List<ThingCategoryRuleConfig> categoryRuleValuesWorkingList;
        private List<string> propertyDeviationKeysWorkingList;
        private List<ThingPropertyDeviationConfig> propertyDeviationValuesWorkingList;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref MaxDescriptionLength, "maxDescriptionLength", 300);
            Scribe_Values.Look(ref FilterDescriptionShorterThanLabel, "filterDescriptionShorterThanLabel", true);
            Scribe_Values.Look(ref DescriptionMinLabelLengthMultiplier, "descriptionMinLabelLengthMultiplier", 3f);
            Scribe_Values.Look(ref FilterFertilizedEggVariants, "filterFertilizedEggVariants", true);
            Scribe_Values.Look(ref FertilizedEggVariantTokens, "fertilizedEggVariantTokens", "fert.,unfert.,fertilized,unfertilized");

            Scribe_Values.Look(ref ImportanceWeightMarketValueLog10, "importanceWeightMarketValueLog10", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightMassLog10, "importanceWeightMassLog10", 0.001f);
            Scribe_Values.Look(ref ImportanceWeightHitPointsLog10, "importanceWeightHitPointsLog10", 0.02f);
            Scribe_Values.Look(ref ImportanceWeightStackLimitIsOne, "importanceWeightStackLimitIsOne", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightSpecialValueScore, "importanceWeightSpecialValueScore", 0.01f);
            Scribe_Values.Look(ref ImportanceWeightNutrition, "importanceWeightNutrition", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightDescriptionLengthRatio, "importanceWeightDescriptionLengthRatio", 0.02f);
            Scribe_Values.Look(ref ImportanceMultiplierCraftable, "importanceMultiplierCraftable", 0.95f);

            Scribe_Values.Look(ref EnableCategoryExtraText, "enableCategoryExtraText", true);
            Scribe_Values.Look(ref EnablePriceFeelingText, "enablePriceFeelingText", true);
            Scribe_Values.Look(ref EnableHitPointsFeelingText, "enableHitPointsFeelingText", true);
            Scribe_Values.Look(ref DebugForceShowDeviation, "debugForceShowDeviation", false);
            Scribe_Values.Look(ref FilterIntermediateBuildStates, "filterIntermediateBuildStates", true);
            Scribe_Values.Look(ref IntermediateBuildStateTokens, "intermediateBuildStateTokens", "blueprint,frame");

            Scribe_Values.Look(ref MaxSemanticLinesGlobal, "maxSemanticLinesGlobal", 8);
            Scribe_Values.Look(ref SpecialValueTopN, "specialValueTopN", 3);

            Scribe_Collections.Look(
                ref CategoryRules,
                "categoryRules",
                LookMode.Value,
                LookMode.Deep,
                ref categoryRuleKeysWorkingList,
                ref categoryRuleValuesWorkingList);

            Scribe_Collections.Look(
                ref PropertyDeviationConfigs,
                "propertyDeviationConfigs",
                LookMode.Value,
                LookMode.Deep,
                ref propertyDeviationKeysWorkingList,
                ref propertyDeviationValuesWorkingList);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (CategoryRules == null)
                {
                    CategoryRules = new Dictionary<string, ThingCategoryRuleConfig>();
                }

                if (PropertyDeviationConfigs == null)
                {
                    PropertyDeviationConfigs = new Dictionary<string, ThingPropertyDeviationConfig>();
                }
            }
        }
    }

    public class ThingCategoryRuleConfig : IExposable
    {
        public bool Enabled = true;
        public string PropertyKeys = string.Empty;
        public int MaxLines = 4;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref PropertyKeys, "propertyKeys", string.Empty);
            Scribe_Values.Look(ref MaxLines, "maxLines", 4);
        }
    }

    public class ThingPropertyDeviationConfig : IExposable
    {
        public bool Enabled = true;
        public float RangeMin = 0f;
        public float RangeMax = 20f;
        public float Scale = 1f;
        public bool NonNegativeOnly = false;
        public bool IsPercent = false;
        public string DisplayName = string.Empty;
        public string StageTextNegStrong = string.Empty;
        public string StageTextNegLight = string.Empty;
        public string StageTextPosLight = string.Empty;
        public string StageTextPosStrong = string.Empty;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref RangeMin, "rangeMin", 0f);
            Scribe_Values.Look(ref RangeMax, "rangeMax", 20f);
            Scribe_Values.Look(ref Scale, "scale", 1f);
            Scribe_Values.Look(ref NonNegativeOnly, "nonNegativeOnly", false);
            Scribe_Values.Look(ref IsPercent, "isPercent", false);
            Scribe_Values.Look(ref DisplayName, "displayName", string.Empty);
            Scribe_Values.Look(ref StageTextNegStrong, "stageTextNegStrong", string.Empty);
            Scribe_Values.Look(ref StageTextNegLight, "stageTextNegLight", string.Empty);
            Scribe_Values.Look(ref StageTextPosLight, "stageTextPosLight", string.Empty);
            Scribe_Values.Look(ref StageTextPosStrong, "stageTextPosStrong", string.Empty);
        }
    }

    public class PawnKindProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeAnimals = true;
        public bool IncludeMechanoids = true;
        public float ImportanceWeightCombatPowerLog10 = 0.05f;
        public float ImportanceWeightTrader = 0.1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeAnimals, "includeAnimals", true);
            Scribe_Values.Look(ref IncludeMechanoids, "includeMechanoids", true);
            Scribe_Values.Look(ref ImportanceWeightCombatPowerLog10, "importanceWeightCombatPowerLog10", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightTrader, "importanceWeightTrader", 0.1f);
        }
    }

    public class TraitProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeDegreeDetails = true;
        public float ImportanceWeightDegreeCount = 0.01f;
        public float ImportanceWeightCommonality = 0f;
        public float ImportanceWeightCommonalityLog10 = -0.1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeDegreeDetails, "includeDegreeDetails", true);
            Scribe_Values.Look(ref ImportanceWeightDegreeCount, "importanceWeightDegreeCount", 0.01f);
            Scribe_Values.Look(ref ImportanceWeightCommonality, "importanceWeightCommonality", 0f);
            Scribe_Values.Look(ref ImportanceWeightCommonalityLog10, "importanceWeightCommonalityLog10", -0.1f);
        }
    }

    public class ResearchProjectProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludePrerequisites = true;
        public bool IncludePostrequisites = true;
        public float ImportanceWeightCost = 0.1f;
        public float ImportanceWeightPrereqCount = 0.03f;
        public float ImportanceWeightPostreqCount = 0.04f;
        public bool UseCostLog10Weight = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludePrerequisites, "includePrerequisites", true);
            Scribe_Values.Look(ref IncludePostrequisites, "includePostrequisites", true);
            Scribe_Values.Look(ref ImportanceWeightCost, "importanceWeightCost", 0.1f);
            Scribe_Values.Look(ref ImportanceWeightPrereqCount, "importanceWeightPrereqCount", 0.03f);
            Scribe_Values.Look(ref ImportanceWeightPostreqCount, "importanceWeightPostreqCount", 0.04f);
            Scribe_Values.Look(ref UseCostLog10Weight, "useCostLog10Weight", true);
        }
    }

    public class RecipeProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeIngredients = true;
        public bool IncludeWorkbench = true;
        public float ImportanceWeightWorkAmountLog10 = 0.1f;
        public float ImportanceWeightIngredientCountLog10 = 0.01f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeIngredients, "includeIngredients", true);
            Scribe_Values.Look(ref IncludeWorkbench, "includeWorkbench", true);
            Scribe_Values.Look(ref ImportanceWeightWorkAmountLog10, "importanceWeightWorkAmountLog10", 0.1f);
            Scribe_Values.Look(ref ImportanceWeightIngredientCountLog10, "importanceWeightIngredientCountLog10", 0.01f);
        }
    }

    public class HediffProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeGoodHediffs = true;
        public bool IncludeImplants = true;
        public float ImportanceWeightIsBad = 0.05f;
        public float ImportanceWeightIsChronic = 0.10f;
        public float ImportanceWeightTendable = -0.03f;
        public float ImportanceWeightIsLethal = 0.15f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeGoodHediffs, "includeGoodHediffs", true);
            Scribe_Values.Look(ref IncludeImplants, "includeImplants", true);
            Scribe_Values.Look(ref ImportanceWeightIsBad, "importanceWeightIsBad", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightIsChronic, "importanceWeightIsChronic", 0.10f);
            Scribe_Values.Look(ref ImportanceWeightTendable, "importanceWeightTendable", -0.03f);
            Scribe_Values.Look(ref ImportanceWeightIsLethal, "importanceWeightIsLethal", 0.15f);
        }
    }

    public class GeneProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeArchiteOnly = false;
        public bool IncludeNegativeGenes = true;
        public float ImportanceWeightBiostatCpx = 0.03f;
        public float ImportanceWeightBiostatMet = 0.04f;
        public float ImportanceWeightBiostatArc = 0.09f;
        public float ImportanceWeightDescriptionLengthLog10 = 0.04f;
        public float ImportanceBonusHasAbilities = 0.15f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeArchiteOnly, "includeArchiteOnly", false);
            Scribe_Values.Look(ref IncludeNegativeGenes, "includeNegativeGenes", true);
            Scribe_Values.Look(ref ImportanceWeightBiostatCpx, "importanceWeightBiostatCpx", 0.03f);
            Scribe_Values.Look(ref ImportanceWeightBiostatMet, "importanceWeightBiostatMet", 0.04f);
            Scribe_Values.Look(ref ImportanceWeightBiostatArc, "importanceWeightBiostatArc", 0.09f);
            Scribe_Values.Look(ref ImportanceWeightDescriptionLengthLog10, "importanceWeightDescriptionLengthLog10", 0.04f);
            Scribe_Values.Look(ref ImportanceBonusHasAbilities, "importanceBonusHasAbilities", 0.15f);
        }
    }

    public class MemeProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludeStructureMemes = true;
        public bool IncludeJokeMemes = false;
        public float ImportanceWeightImpact = 0.1f;
        public float ImportanceWeightCategory = 0.02f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeStructureMemes, "includeStructureMemes", true);
            Scribe_Values.Look(ref IncludeJokeMemes, "includeJokeMemes", false);
            Scribe_Values.Look(ref ImportanceWeightImpact, "importanceWeightImpact", 0.1f);
            Scribe_Values.Look(ref ImportanceWeightCategory, "importanceWeightCategory", 0.02f);
        }
    }

    public class FactionProcessDefConfig : ProcessDefBaseConfig
    {
        public bool IncludePlayerFaction = false;
        public bool IncludeHiddenFactions = false;
        public float ImportanceWeightTechLevel = 0.05f;
        public float ImportanceWeightPermanentEnemy = 0.075f;
        public float ImportanceWeightHumanlike = 0.03f;
        public string TechLevelScoreMap = "Undefined:0,Animal:1,Neolithic:2,Medieval:3,Industrial:4,Spacer:5,Ultra:6,Archotech:7";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludePlayerFaction, "includePlayerFaction", false);
            Scribe_Values.Look(ref IncludeHiddenFactions, "includeHiddenFactions", false);
            Scribe_Values.Look(ref ImportanceWeightTechLevel, "importanceWeightTechLevel", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightPermanentEnemy, "importanceWeightPermanentEnemy", 0.075f);
            Scribe_Values.Look(ref ImportanceWeightHumanlike, "importanceWeightHumanlike", 0.03f);
            Scribe_Values.Look(ref TechLevelScoreMap, "techLevelScoreMap", "Undefined:0,Animal:1,Neolithic:2,Medieval:3,Industrial:4,Spacer:5,Ultra:6,Archotech:7");
        }
    }
}
