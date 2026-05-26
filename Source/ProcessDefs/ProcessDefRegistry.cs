using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefRegistry
    {
        private static readonly IReadOnlyList<IProcessDef> CachedProcessors = new ReadOnlyCollection<IProcessDef>(new List<IProcessDef>
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
        });

        public static IReadOnlyList<IProcessDef> GetProcessors()
        {
            return CachedProcessors;
        }
    }
}
