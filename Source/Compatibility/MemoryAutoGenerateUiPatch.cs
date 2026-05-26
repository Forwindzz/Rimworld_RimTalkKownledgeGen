using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.Compatibility
{
    public static class MemoryAutoGenerateUiPatch
    {
        private const string DialogTypeName = "RimTalk.Memory.UI.Dialog_CommonKnowledge";

        private static bool patchAttempted;
        private static bool drawHookLoggedOnce;

        public static void TryApply(Harmony harmony)
        {
            if (patchAttempted)
            {
                return;
            }

            patchAttempted = true;
            if (harmony == null)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed: Harmony instance is null.");
                return;
            }

            try
            {
                Type dialogType = AccessTools.TypeByName(DialogTypeName);
                if (dialogType == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: target type not found: " + DialogTypeName);
                    return;
                }

                MethodInfo target = AccessTools.Method(dialogType, "DrawSidebar", new[] { typeof(Rect) });
                if (target == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: target method not found: " + DialogTypeName + ".DrawSidebar(Rect).");
                    return;
                }

                MethodInfo postfix = AccessTools.Method(typeof(MemoryAutoGenerateUiPatch), nameof(PostfixDrawSidebar));
                if (postfix == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: postfix method not found.");
                    return;
                }

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Log.Message("[GenKnowledge] Memory UI postfix patch applied: " + DialogTypeName + ".DrawSidebar");
            }
            catch (Exception ex)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed with exception: " + ex);
            }
        }

        private static void PostfixDrawSidebar(Rect rect)
        {
            if (!(GenKnowledgeMod.Settings?.enableMemoryUiPatch ?? true))
            {
                return;
            }

            if (!drawHookLoggedOnce)
            {
                drawHookLoggedOnce = true;
                Log.Message("[GenKnowledge] Memory UI draw hook entered.");
            }

            Rect innerRect = rect.ContractedBy(8f);
            const float rowHeight = 24f;
            const float gap = 4f;
            const float bottomReserve = 70f;
            const float bottomOffset = 96f;

            // Always append in sidebar footer region so it does not depend on foldout expansion.
            float y = innerRect.yMax - bottomReserve - bottomOffset - (rowHeight * 2f + gap);
            y = Mathf.Max(innerRect.y, y);
            Rect defsGenerateRect = new Rect(innerRect.x, y, innerRect.width, rowHeight);
            if (Widgets.ButtonText(defsGenerateRect, "RimTalkGenKnowledge.Memory.Button.GenerateFromDefs".Translate()))
            {
                RunDefsGeneration();
            }

            y += rowHeight + gap;
            Rect openPanelRect = new Rect(innerRect.x, y, innerRect.width, rowHeight);
            if (Widgets.ButtonText(openPanelRect, "RimTalkGenKnowledge.Memory.Button.OpenDefsPanel".Translate()))
            {
                OpenDefsSettingsPanel();
            }
        }

        private static void RunDefsGeneration()
        {
            if (Current.Game == null || Find.World == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenerationOnlyInLoadedSave".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            GenKnowledgeGameComponent comp = Current.Game.GetComponent<GenKnowledgeGameComponent>();
            if (comp == null)
            {
                Messages.Message("RimTalkGenKnowledge.Message.GenerationFailedMissingComponent".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            bool reportErrors = GenKnowledgeMod.Settings?.enableGlobalErrorReporting ?? false;
            GenerationReport report = comp.RunGeneration(reportErrors);
            MessageTypeDef type = report.FailedCount > 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.TaskCompletion;
            Messages.Message(report.BuildSummaryLine(), type, false);
        }

        private static void OpenDefsSettingsPanel()
        {
            GenKnowledgeMod mod = LoadedModManager.GetMod<GenKnowledgeMod>();
            if (mod == null)
            {
                Messages.Message("RimTalkGenKnowledge.Memory.Message.ModNotLoaded".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            try
            {
                Find.WindowStack.Add(new Dialog_ModSettings(mod));
            }
            catch (Exception ex)
            {
                Messages.Message("RimTalkGenKnowledge.Memory.Message.OpenSettingsFailed".Translate(ex.Message), MessageTypeDefOf.RejectInput, false);
            }
        }

    }
}
