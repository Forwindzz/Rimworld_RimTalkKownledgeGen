using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace GenKnowledge
{
    public class KnowledgeApiBridge
    {
        private const string TargetAssemblyName = "RimTalkMemoryPatch";
        private const string TargetTypeName = "RimTalk.Memory.CommonKnowledgeAPI";
        private const string MatchModeTypeName = "RimTalk.Memory.KeywordMatchMode";

        private readonly bool reportEachError;
        private readonly bool allowExtraction;
        private readonly bool allowMatching;
        private Assembly targetAssembly;
        private Type apiType;
        private Type keywordMatchModeType;
        private MethodInfo addKnowledgeExMethod;
        private MethodInfo updateKnowledgeMethod;
        private MethodInfo removeKnowledgeMethod;
        private MethodInfo findKnowledgeByIdMethod;

        public bool IsReady { get; private set; }
        public string LastInitError { get; private set; }

        public KnowledgeApiBridge(bool reportEachError, bool allowExtraction, bool allowMatching)
        {
            this.reportEachError = reportEachError;
            this.allowExtraction = allowExtraction;
            this.allowMatching = allowMatching;
        }

        public bool Initialize()
        {
            try
            {
                targetAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == TargetAssemblyName);
                if (targetAssembly == null)
                {
                    return FailInit("RimTalkGenKnowledge.Message.AssemblyNotFound".Translate(TargetAssemblyName));
                }

                apiType = targetAssembly.GetType(TargetTypeName);
                if (apiType == null)
                {
                    return FailInit("RimTalkGenKnowledge.Message.TypeNotFound".Translate(TargetTypeName));
                }

                keywordMatchModeType = targetAssembly.GetType(MatchModeTypeName);
                if (keywordMatchModeType == null || !keywordMatchModeType.IsEnum)
                {
                    return FailInit("RimTalkGenKnowledge.Message.TypeNotFound".Translate(MatchModeTypeName));
                }

                addKnowledgeExMethod = apiType.GetMethod("AddKnowledgeEx", BindingFlags.Public | BindingFlags.Static);
                updateKnowledgeMethod = apiType.GetMethod("UpdateKnowledge", BindingFlags.Public | BindingFlags.Static);
                removeKnowledgeMethod = apiType.GetMethod("RemoveKnowledge", BindingFlags.Public | BindingFlags.Static);
                findKnowledgeByIdMethod = apiType.GetMethod("FindKnowledgeById", BindingFlags.Public | BindingFlags.Static);

                if (addKnowledgeExMethod == null || updateKnowledgeMethod == null || removeKnowledgeMethod == null || findKnowledgeByIdMethod == null)
                {
                    return FailInit("RimTalkGenKnowledge.Message.ApiMethodsMissing".Translate());
                }

                ParameterInfo[] addParams = addKnowledgeExMethod.GetParameters();
                if (addParams.Length != 7)
                {
                    return FailInit("RimTalkGenKnowledge.Message.AddKnowledgeExSignatureMismatch".Translate());
                }

                IsReady = true;
                LastInitError = null;
                return true;
            }
            catch (Exception ex)
            {
                return FailInit("RimTalkGenKnowledge.Message.InitializationException".Translate(ex.Message));
            }
        }

        public string AddKnowledge(string tag, string content, float importance, GenerationReport report)
        {
            if (!EnsureReady(report))
            {
                return null;
            }

            try
            {
                object matchModeAny = Enum.Parse(keywordMatchModeType, "Any", true);
                object result = addKnowledgeExMethod.Invoke(null, new object[]
                {
                    tag,
                    content,
                    importance,
                    matchModeAny,
                    -1,
                    allowExtraction,
                    allowMatching
                });
                return result as string;
            }
            catch (Exception ex)
            {
                ReportError("RimTalkGenKnowledge.Message.AddKnowledgeExFailed".Translate(ex.Message), report);
                return null;
            }
        }

        public bool UpdateKnowledge(string id, string newContent, GenerationReport report)
        {
            if (!EnsureReady(report))
            {
                return false;
            }

            try
            {
                object result = updateKnowledgeMethod.Invoke(null, new object[] { id, newContent });
                return result is bool ok && ok;
            }
            catch (Exception ex)
            {
                ReportError("RimTalkGenKnowledge.Message.UpdateKnowledgeFailed".Translate(ex.Message), report);
                return false;
            }
        }

        public bool RemoveKnowledge(string id, GenerationReport report)
        {
            if (!EnsureReady(report))
            {
                return false;
            }

            try
            {
                object result = removeKnowledgeMethod.Invoke(null, new object[] { id });
                return result is bool ok && ok;
            }
            catch (Exception ex)
            {
                ReportError("RimTalkGenKnowledge.Message.RemoveKnowledgeFailed".Translate(ex.Message), report);
                return false;
            }
        }

        public bool ExistsKnowledge(string id, GenerationReport report)
        {
            if (!EnsureReady(report))
            {
                return false;
            }

            try
            {
                object result = findKnowledgeByIdMethod.Invoke(null, new object[] { id });
                return result != null;
            }
            catch (Exception ex)
            {
                ReportError("RimTalkGenKnowledge.Message.FindKnowledgeByIdFailed".Translate(ex.Message), report);
                return false;
            }
        }

        private bool EnsureReady(GenerationReport report)
        {
            if (IsReady)
            {
                return true;
            }

            string error = string.IsNullOrWhiteSpace(LastInitError)
                ? "RimTalkGenKnowledge.Message.ApiBridgeNotInitialized".Translate().ToString()
                : LastInitError;
            ReportError(error, report);
            return false;
        }

        private bool FailInit(string error)
        {
            IsReady = false;
            LastInitError = error;
            Log.Error($"[GenKnowledge] {error}");
            return false;
        }

        private void ReportError(string error, GenerationReport report)
        {
            report?.AddError(error);
            Log.Error($"[GenKnowledge] {error}");

            if (reportEachError)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenKnowledgeError".Translate(error), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
