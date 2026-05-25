using System.Collections.Generic;
using UnityEngine;

namespace GenKnowledge.ProcessDefs
{
    public interface IProcessDef
    {
        string Id { get; }
        string DisplayName { get; }

        ProcessDefBaseConfig CreateDefaultConfig();
        float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth);
        void DrawConfig(Rect rect, ProcessDefBaseConfig config);

        IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config);
    }
}
