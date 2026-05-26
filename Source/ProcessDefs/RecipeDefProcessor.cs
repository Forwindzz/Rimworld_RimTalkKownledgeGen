using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GenKnowledge.ProcessDefs
{
    public class RecipeDefProcessor : ProcessDefProcessorBase<RecipeProcessDefConfig>
    {
        public const string ProcessorId = "RecipeDefProcessor";

        public override string Id => ProcessorId;
        public override string DisplayName => "RimTalkGenKnowledge.Processor.Recipe.DisplayName".Translate();

        public override ProcessDefBaseConfig CreateDefaultConfig()
        {
            return new RecipeProcessDefConfig
            {
                Enabled = true,
                IncludeModDefs = true,
                TagTemplate = "RimTalkGenKnowledge.DefaultTemplate.Recipe.Tag".Translate(),
                KnowledgeTemplate = "RimTalkGenKnowledge.DefaultTemplate.Recipe.Knowledge".Translate(),
                BaseImportance = 0.1f,
                ImportanceMin = 0.01f,
                ImportanceMax = 0.7f,
                IncludeIngredients = true,
                IncludeWorkbench = true,
                ImportanceWeightWorkAmountLog10 = 0.1f,
                ImportanceWeightIngredientCountLog10 = 0.01f
            };
        }

        public override void ApplyDefaultConfig(ProcessDefBaseConfig config)
        {
            RecipeProcessDefConfig typed = config as RecipeProcessDefConfig;
            if (typed == null)
            {
                return;
            }

            RecipeProcessDefConfig defaults = (RecipeProcessDefConfig)CreateDefaultConfig();
            CopyBaseConfigFields(defaults, typed);
            typed.IncludeIngredients = defaults.IncludeIngredients;
            typed.IncludeWorkbench = defaults.IncludeWorkbench;
            typed.ImportanceWeightWorkAmountLog10 = defaults.ImportanceWeightWorkAmountLog10;
            typed.ImportanceWeightIngredientCountLog10 = defaults.ImportanceWeightIngredientCountLog10;
        }

        public override IEnumerable<PlaceholderDescriptor> GetPlaceholders()
        {
            return new[]
            {
                new PlaceholderDescriptor { Key = "label", Token = "{{label}}", Description = "Recipe label" },
                new PlaceholderDescriptor { Key = "description", Token = "{{description}}", Description = "Recipe description" },
                new PlaceholderDescriptor { Key = "workAmount", Token = "{{workAmount}}", Description = "Work amount" },
                new PlaceholderDescriptor { Key = "workSkill", Token = "{{workSkill}}", Description = "Work skill" },
                new PlaceholderDescriptor { Key = "workbench", Token = "{{workbench}}", Description = "Work benches" },
                new PlaceholderDescriptor { Key = "ingredients", Token = "{{ingredients}}", Description = "Ingredients summary" },
                new PlaceholderDescriptor { Key = "defName", Token = "{{defName}}", Description = "Def name" }
            };
        }

        public override float GetConfigHeight(ProcessDefBaseConfig config, float viewWidth)
        {
            return 520f;
        }

        protected override float DrawAdvancedConfig(float x, float y, float width, float lineHeight, float gap, RecipeProcessDefConfig config)
        {
            Rect ingRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(ingRect, "RimTalkGenKnowledge.Settings.Recipe.IncludeIngredients".Translate(), ref config.IncludeIngredients);
            y += lineHeight + gap;
            Rect benchRect = new Rect(x, y, width, lineHeight);
            Widgets.CheckboxLabeled(benchRect, "RimTalkGenKnowledge.Settings.Recipe.IncludeWorkbench".Translate(), ref config.IncludeWorkbench);
            y += lineHeight + gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Recipe.WeightWorkAmountLog10".Translate(), config.ImportanceWeightWorkAmountLog10, v => config.ImportanceWeightWorkAmountLog10 = v);
            y += gap;
            y = ProcessDefUiUtility.DrawFloatRow(x, y, width, lineHeight, "RimTalkGenKnowledge.Settings.Recipe.WeightIngredientCountLog10".Translate(), config.ImportanceWeightIngredientCountLog10, v => config.ImportanceWeightIngredientCountLog10 = v);
            y += gap;
            return y;
        }

        public override IEnumerable<GeneratedKnowledgeItem> ProcessDefs(ProcessDefContext context, ProcessDefBaseConfig config)
        {
            RecipeProcessDefConfig typed = config as RecipeProcessDefConfig ?? (RecipeProcessDefConfig)CreateDefaultConfig();
            if (!typed.Enabled)
            {
                yield break;
            }

            foreach (RecipeDef def in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.defName))
                {
                    continue;
                }

                if (!ProcessDefUtility.ShouldIncludeDef(def, typed.IncludeModDefs))
                {
                    continue;
                }

                string label = ProcessDefUtility.TrimOrNull(def.label);
                string description = ProcessDefUtility.TrimOrNull(def.description);
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                if (!ProcessDefUtility.MeetsDescriptionLengthThreshold(label, description, 3f))
                {
                    continue;
                }

                string workbench = GetRecipeUsersLabel(def.recipeUsers);
                if (typed.IncludeWorkbench && string.IsNullOrWhiteSpace(workbench))
                {
                    continue;
                }

                float workAmount = def.workAmount;
                float ingredientCount = GetTotalIngredientCount(def.ingredients);
                string ingredientsSummary = typed.IncludeIngredients ? ingredientCount.ToString("0.##") : string.Empty;

                SetTemplateValues(new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["description"] = description,
                    ["workAmount"] = workAmount.ToString("0.##"),
                    ["workSkill"] = def.workSkill?.label ?? string.Empty,
                    ["workbench"] = workbench,
                    ["ingredients"] = ingredientsSummary,
                    ["defName"] = def.defName
                });

                string tag = RenderTag(typed);
                string content = RenderContent(typed);
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                float workAmountMetric = ProcessDefUtility.SafeLog10(workAmount);
                float ingredientMetric = ProcessDefUtility.SafeLog10(ingredientCount);
                float raw = typed.BaseImportance
                    + Math.Abs(workAmountMetric) * typed.ImportanceWeightWorkAmountLog10
                    + Math.Abs(ingredientMetric) * typed.ImportanceWeightIngredientCountLog10;

                yield return new GeneratedKnowledgeItem
                {
                    LogicalKey = "recipe:" + def.defName,
                    Tag = tag,
                    Content = content,
                    Importance = ComputeFinalImportance(raw, typed)
                };
            }
        }

        private static string GetRecipeUsersLabel(List<ThingDef> users)
        {
            if (users == null || users.Count == 0)
            {
                return string.Empty;
            }

            var labels = new List<string>();
            for (int i = 0; i < users.Count; i++)
            {
                string label = ProcessDefUtility.TrimOrNull(users[i]?.label);
                if (!string.IsNullOrWhiteSpace(label))
                {
                    labels.Add(label);
                }
            }

            return string.Join(",", labels.ToArray());
        }

        private static float GetTotalIngredientCount(IList ingredients)
        {
            if (ingredients == null)
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < ingredients.Count; i++)
            {
                object ingredient = ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                Type type = ingredient.GetType();
                var method = type.GetMethod("GetBaseCount");
                if (method != null)
                {
                    object v = method.Invoke(ingredient, null);
                    if (v != null)
                    {
                        sum += Convert.ToSingle(v);
                        continue;
                    }
                }

                var field = type.GetField("baseCount");
                if (field != null)
                {
                    object v = field.GetValue(ingredient);
                    if (v != null)
                    {
                        sum += Convert.ToSingle(v);
                        continue;
                    }
                }

                var prop = type.GetProperty("BaseCount");
                if (prop != null)
                {
                    object v = prop.GetValue(ingredient, null);
                    if (v != null)
                    {
                        sum += Convert.ToSingle(v);
                    }
                }
            }

            return sum;
        }
    }
}
