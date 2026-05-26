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
        private const string HelperTypeName = "RimTalk.Memory.UI.CommonKnowledgeUIHelpers";
        private const string MemorySettingsModTypeName = "RimTalk.MemoryPatch.RimTalkMemoryPatchMod";

        private static bool patchAttempted;
        private static bool drawHookLoggedOnce;

        private static Type cachedMemorySettingsType;
        private static PropertyInfo cachedMemorySettingsProperty;
        private static FieldInfo cachedEnablePawnStatusField;
        private static FieldInfo cachedEnableEventRecordField;
        private static bool reflectionInitialized;

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
                Type helperType = AccessTools.TypeByName(HelperTypeName);
                if (helperType == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: target type not found: " + HelperTypeName);
                    return;
                }

                MethodInfo target = AccessTools.Method(helperType, "DrawAutoGenerateSettings", new[] { typeof(Rect), typeof(Action), typeof(Action) });
                if (target == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: target method not found: " + HelperTypeName + ".DrawAutoGenerateSettings(Rect,Action,Action).");
                    return;
                }

                MethodInfo postfix = AccessTools.Method(typeof(MemoryAutoGenerateUiPatch), nameof(PostfixDrawAutoGenerateSettings));
                if (postfix == null)
                {
                    Log.Error("[GenKnowledge] Memory UI patch failed: postfix method not found.");
                    return;
                }

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Log.Message("[GenKnowledge] Memory UI postfix patch applied: " + HelperTypeName + ".DrawAutoGenerateSettings");
            }
            catch (Exception ex)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed with exception: " + ex);
            }
        }

        private static void PostfixDrawAutoGenerateSettings(Rect rect, Action onGeneratePawnStatus, Action onGenerateEventRecord)
        {
            if (!drawHookLoggedOnce)
            {
                drawHookLoggedOnce = true;
                Log.Message("[GenKnowledge] Memory UI draw hook entered.");
            }

            if (!TryInitMemorySettingsReflection())
            {
                return;
            }

            object settingsObj = cachedMemorySettingsProperty.GetValue(null, null);
            if (settingsObj == null)
            {
                return;
            }

            Rect innerRect = rect.ContractedBy(5f);
            float y = innerRect.y;
            const float rowHeight = 24f;
            const float gap = 4f;
            float halfWidth = (innerRect.width - 6f) / 2f;

            // Skip pawn checkbox row, overlay only on the generate button row.
            y += rowHeight + gap;

            Rect defsGenerateRect = new Rect(innerRect.x + halfWidth + 6f, y, halfWidth, rowHeight);
            if (Widgets.ButtonText(defsGenerateRect, "RimTalkGenKnowledge.Memory.Button.GenerateFromDefs".Translate()))
            {
                RunDefsGeneration();
            }

            // Skip event checkbox row.
            y += rowHeight + gap;
            y += rowHeight + gap;

            Rect openPanelRect = new Rect(innerRect.x + halfWidth + 6f, y, halfWidth, rowHeight);
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

        private static bool TryInitMemorySettingsReflection()
        {
            if (reflectionInitialized)
            {
                return cachedMemorySettingsProperty != null &&
                    cachedEnablePawnStatusField != null &&
                    cachedEnableEventRecordField != null;
            }

            reflectionInitialized = true;

            Type modType = AccessTools.TypeByName(MemorySettingsModTypeName);
            if (modType == null)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed: settings mod type not found: " + MemorySettingsModTypeName);
                return false;
            }

            cachedMemorySettingsProperty = AccessTools.Property(modType, "Settings");
            cachedMemorySettingsType = cachedMemorySettingsProperty?.PropertyType;
            if (cachedMemorySettingsType == null)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed: RimTalk settings property type is null.");
                return false;
            }

            cachedEnablePawnStatusField = AccessTools.Field(cachedMemorySettingsType, "enablePawnStatusKnowledge");
            cachedEnableEventRecordField = AccessTools.Field(cachedMemorySettingsType, "enableEventRecordKnowledge");

            if (cachedEnablePawnStatusField == null || cachedEnableEventRecordField == null)
            {
                Log.Error("[GenKnowledge] Memory UI patch failed: RimTalk settings fields not found.");
                return false;
            }

            return true;
        }
    }
}
