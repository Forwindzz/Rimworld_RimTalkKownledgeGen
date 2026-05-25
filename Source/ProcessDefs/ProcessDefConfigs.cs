using Verse;

namespace GenKnowledge.ProcessDefs
{
    public abstract class ProcessDefBaseConfig : IExposable
    {
        public bool Enabled = true;
        public string TagTemplate = "{{label}}";
        public string KnowledgeTemplate = "{{label}}: {{description}}";
        public float BaseImportance = 0.5f;

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref TagTemplate, "tagTemplate", "{{label}}");
            Scribe_Values.Look(ref KnowledgeTemplate, "knowledgeTemplate", "{{label}}: {{description}}");
            Scribe_Values.Look(ref BaseImportance, "baseImportance", 0.5f);
        }
    }

    public class XenotypeProcessDefConfig : ProcessDefBaseConfig
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
