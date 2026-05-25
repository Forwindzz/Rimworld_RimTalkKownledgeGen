using System.Collections.Generic;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefRegistry
    {
        public static List<IProcessDef> CreateProcessors()
        {
            return new List<IProcessDef>
            {
                new XenotypeDefProcessor(),
                new ThingDefProcessor(),
                new PawnKindDefProcessor(),
                new TraitDefProcessor(),
                new ResearchProjectDefProcessor(),
                new RecipeDefProcessor(),
                new HediffDefProcessor(),
                new GeneDefProcessor(),
                new MemeDefProcessor(),
                new FactionDefProcessor()
            };
        }
    }
}
