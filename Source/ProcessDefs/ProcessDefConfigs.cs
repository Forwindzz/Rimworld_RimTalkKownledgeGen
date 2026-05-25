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
        public string IncludeCategories = "Weapon,Apparel,Medicine,Food,Building";
        public string ExcludeCategories = string.Empty;
        public int MaxDescriptionLength = 300;
        public bool IncludeStatSummary = false;
        public float ImportanceWeightMarketValueLog10 = 0.05f;
        public float ImportanceWeightMassLog10 = 0.001f;
        public float ImportanceWeightStackLimitIsOne = 0.05f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeCategories, "includeCategories", "Weapon,Apparel,Medicine,Food,Building");
            Scribe_Values.Look(ref ExcludeCategories, "excludeCategories", string.Empty);
            Scribe_Values.Look(ref MaxDescriptionLength, "maxDescriptionLength", 300);
            Scribe_Values.Look(ref IncludeStatSummary, "includeStatSummary", false);
            Scribe_Values.Look(ref ImportanceWeightMarketValueLog10, "importanceWeightMarketValueLog10", 0.05f);
            Scribe_Values.Look(ref ImportanceWeightMassLog10, "importanceWeightMassLog10", 0.001f);
            Scribe_Values.Look(ref ImportanceWeightStackLimitIsOne, "importanceWeightStackLimitIsOne", 0.05f);
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
        public float ImportanceWeightCost = 0.1f;
        public float ImportanceWeightPrereqCount = 0.03f;
        public bool UseCostLog10Weight = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludePrerequisites, "includePrerequisites", true);
            Scribe_Values.Look(ref ImportanceWeightCost, "importanceWeightCost", 0.1f);
            Scribe_Values.Look(ref ImportanceWeightPrereqCount, "importanceWeightPrereqCount", 0.03f);
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
        public float ImportanceWeightBiostatArc = 0.15f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref IncludeArchiteOnly, "includeArchiteOnly", false);
            Scribe_Values.Look(ref IncludeNegativeGenes, "includeNegativeGenes", true);
            Scribe_Values.Look(ref ImportanceWeightBiostatCpx, "importanceWeightBiostatCpx", 0.03f);
            Scribe_Values.Look(ref ImportanceWeightBiostatMet, "importanceWeightBiostatMet", 0.04f);
            Scribe_Values.Look(ref ImportanceWeightBiostatArc, "importanceWeightBiostatArc", 0.15f);
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
