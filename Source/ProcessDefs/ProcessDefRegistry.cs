using System.Collections.Generic;

namespace GenKnowledge.ProcessDefs
{
    public static class ProcessDefRegistry
    {
        public static List<IProcessDef> CreateProcessors()
        {
            return new List<IProcessDef>
            {
                new XenotypeDefProcessor()
            };
        }
    }
}
