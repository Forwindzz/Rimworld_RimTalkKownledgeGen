using System.Collections.Generic;
using UnityEngine;

namespace GenKnowledge.ProcessDefs
{
    public class PlaceholderDescriptor
    {
        public string Key;
        public string Token;
        public string Description;
        public string ExampleValue;
    }

    public interface IProcessDef
    {
        string Id { get; }
        string DisplayName { get; }

        ProcessDefBaseConfig CreateDefaultConfig();
        void ApplyDefaultConfig(ProcessDefBaseConfig config);
        IEnumerable<PlaceholderDescriptor> GetPlaceholders();
        string ProcessTemplateString(string templateString, ProcessDefBaseConfig config);
        float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth);
        void DrawConfig(Rect rect, ProcessDefBaseConfig config);

        IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config);
    }
}
