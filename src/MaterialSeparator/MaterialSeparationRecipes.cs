using System.Collections.Generic;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.MaterialSeparator
{
    internal static class MaterialSeparationRecipes
    {
        private const string RecipeCategory = "MaterialSeparation";
        private const string RecipeCategoryPrefix =
            CraftingTableConfig.ID + "_" + RecipeCategory + "_";
        private const float SeparationTimeSeconds = 10f;

        private static readonly HashSet<string> reverseRecipeIds =
            new HashSet<string>();

        internal static bool IsReverseRecipe(ComplexRecipe recipe)
        {
            return recipe != null && (reverseRecipeIds.Contains(recipe.id) ||
                (!string.IsNullOrEmpty(recipe.recipeCategoryID) &&
                    recipe.recipeCategoryID.StartsWith(
                        RecipeCategoryPrefix,
                        System.StringComparison.Ordinal)));
        }

        internal static bool IsRepairRecipe(ComplexRecipe recipe)
        {
            if (recipe == null || recipe.ingredients == null)
                return false;

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (ingredient == null)
                    continue;

                if (IsWornItem(ingredient.material))
                    return true;

                if (ingredient.possibleMaterials != null)
                    foreach (Tag possible in ingredient.possibleMaterials)
                        if (IsWornItem(possible))
                            return true;
            }
            return false;
        }

        internal static void Register(ComplexRecipeManager manager)
        {
            reverseRecipeIds.Clear();
            if (manager == null || manager.recipes == null)
                return;

            manager.recipes.RemoveAll(IsReverseRecipe);
            manager.preProcessRecipes.RemoveWhere(IsReverseRecipe);
            if (!ModSettings.Current.EnableMaterialSeparator)
                return;

            var sourceRecipes = new List<ComplexRecipe>(manager.recipes);
            var registeredProducts = new HashSet<Tag>();
            int sortOrder = 1000;

            foreach (ComplexRecipe source in sourceRecipes)
            {
                if (!TryCreateReverseRecipe(source, registeredProducts,
                    sortOrder, out ComplexRecipe reverse))
                    continue;

                int firstAddedRecipe = manager.recipes.Count;
                manager.Add(reverse, true);
                manager.preProcessRecipes.Remove(reverse);

                for (int i = firstAddedRecipe; i < manager.recipes.Count; i++)
                {
                    ComplexRecipe added = manager.recipes[i];
                    reverseRecipeIds.Add(added.id);
                    added.RequiresAllIngredientsDiscovered = true;
                }

                sortOrder++;
            }

            Debug.Log("[ONIUtilityTweaks] Added " + reverseRecipeIds.Count +
                " material separation recipes to the Crafting Station.");
        }

        private static bool TryCreateReverseRecipe(
            ComplexRecipe source,
            ISet<Tag> registeredProducts,
            int sortOrder,
            out ComplexRecipe reverse)
        {
            reverse = null;
            if (!RecipeCatalog.IsReversible(source) || IsReverseRecipe(source))
                return false;

            ComplexRecipe.RecipeElement product = source.results[0];
            GameObject productPrefab = Assets.TryGetPrefab(product.material);
            if (!product.material.IsValid || productPrefab == null ||
                productPrefab.GetComponent<Pickupable>() == null)
                return false;

            Dictionary<Tag, float> yields = MaterialRecovery.CaptureIngredients(source, null);
            if (yields.Count == 0 || registeredProducts.Contains(product.material))
                return false;

            var inputs = new[]
            {
                new ComplexRecipe.RecipeElement(
                    product.material,
                    product.amount,
                    ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature)
            };
            var outputs = new ComplexRecipe.RecipeElement[yields.Count];
            int outputIndex = 0;
            foreach (KeyValuePair<Tag, float> yield in yields)
            {
                outputs[outputIndex++] = new ComplexRecipe.RecipeElement(
                    yield.Key,
                    yield.Value,
                    ComplexRecipe.RecipeElement.TemperatureOperation.AverageTemperature);
            }

            string productName = productPrefab.GetProperName();
            reverse = new ComplexRecipe(
                ComplexRecipeManager.MakeRecipeID(CraftingTableConfig.ID, inputs, outputs),
                inputs,
                outputs)
            {
                time = SeparationTimeSeconds,
                description = "Recover the materials used to craft " + productName + ".",
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Custom,
                customName = "Separate " + productName,
                customSpritePrefabID = product.material.Name,
                fabricators = new List<Tag> { CraftingTableConfig.ID },
                requiredTech = source.requiredTech,
                sortOrder = sortOrder,
                recipeCategoryID = ComplexRecipeManager.MakeRecipeCategoryID(
                    CraftingTableConfig.ID, RecipeCategory, product.material.Name)
            };
            reverse.SetDLCRestrictions(source.GetRequiredDlcIds(), source.GetForbiddenDlcIds());
            registeredProducts.Add(product.material);
            return true;
        }

        private static bool IsWornItem(Tag tag)
        {
            return tag.IsValid && tag.Name.StartsWith("Worn_",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }

    [HarmonyPatch(typeof(ComplexRecipeManager), nameof(ComplexRecipeManager.PostProcess))]
    internal static class MaterialSeparationRecipeRegistrationPatch
    {
        private static void Postfix(ComplexRecipeManager __instance)
        {
            MaterialSeparationRecipes.Register(__instance);
        }
    }

    [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
    internal static class MaterialSeparationOutputPatch
    {
        private sealed class SeparationState
        {
            public Dictionary<Tag, float> Yields;
            public float Temperature;
        }

        private static void Prefix(
            ComplexRecipe recipe,
            Storage ___buildStorage,
            out SeparationState __state)
        {
            __state = null;
            if (!MaterialSeparationRecipes.IsReverseRecipe(recipe) ||
                ___buildStorage == null || recipe.ingredients == null ||
                recipe.ingredients.Length == 0)
                return;

            GameObject item = ___buildStorage.FindFirst(recipe.ingredients[0].material);
            if (item == null || !MaterialRecovery.TryGetYields(item,
                out Dictionary<Tag, float> yields))
                return;

            PrimaryElement element = item.GetComponent<PrimaryElement>();
            __state = new SeparationState
            {
                Yields = yields,
                Temperature = element == null ? 293.15f : element.Temperature
            };
        }

        private static void Postfix(
            ComplexFabricator __instance,
            Storage ___outStorage,
            List<GameObject> __result,
            SeparationState __state)
        {
            if (__state == null)
                return;

            Vector3 outputPosition = __instance.transform.position + Vector3.up * 0.5f;
            var remainingYields = new Dictionary<Tag, float>(__state.Yields);
            var keptOutputs = new List<GameObject>();
            if (__result != null)
            {
                foreach (GameObject output in __result)
                {
                    if (output == null)
                        continue;

                    outputPosition = output.transform.position;
                    KPrefabID prefabId = output.GetComponent<KPrefabID>();
                    if (prefabId != null && remainingYields.TryGetValue(
                        prefabId.PrefabTag, out float recoveredAmount))
                    {
                        PrimaryElement element = output.GetComponent<PrimaryElement>();
                        if (element != null)
                        {
                            element.Units = recoveredAmount;
                            element.Temperature = __state.Temperature;
                        }
                        remainingYields.Remove(prefabId.PrefabTag);
                        keptOutputs.Add(output);
                        continue;
                    }

                    if (Contains(___outStorage, output))
                        ___outStorage.Remove(output, true);
                    Util.KDestroyGameObject(output);
                }
                __result.Clear();
                __result.AddRange(keptOutputs);
            }

            List<GameObject> recovered = MaterialRecovery.SpawnAll(
                remainingYields, __state.Temperature, outputPosition);
            if (__result != null)
                __result.AddRange(recovered);
        }

        private static bool Contains(Storage storage, GameObject target)
        {
            if (storage == null)
                return false;

            foreach (GameObject item in storage.GetItems())
                if (item == target)
                    return true;
            return false;
        }
    }
}
