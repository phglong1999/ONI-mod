using System.Collections.Generic;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.MaterialSeparator
{
    [HarmonyPatch(typeof(ComplexFabricator), "SpawnOrderProduct")]
    internal static class CraftedRecipeTrackingPatch
    {
        private sealed class CraftSnapshot
        {
            public Dictionary<Tag, float> Ingredients;
            public Tag ProductTag;
            public float ProductAmount;
        }

        private static void Prefix(ComplexRecipe recipe, Storage ___buildStorage, out CraftSnapshot __state)
        {
            __state = Capture(recipe, ___buildStorage);
        }

        private static void Postfix(List<GameObject> __result, CraftSnapshot __state)
        {
            if (__state == null || __result == null)
                return;

            foreach (GameObject product in __result)
            {
                KPrefabID prefabId = product == null ? null : product.GetComponent<KPrefabID>();
                if (prefabId == null || prefabId.PrefabTag != __state.ProductTag)
                    continue;

                CraftedRecipeOrigin origin = product.AddOrGet<CraftedRecipeOrigin>();
                origin.Record(__state.ProductAmount, __state.Ingredients);
            }
        }

        private static CraftSnapshot Capture(ComplexRecipe recipe, Storage buildStorage)
        {
            if (!ModSettings.Current.EnableMaterialSeparator ||
                !RecipeCatalog.IsReversible(recipe) ||
                MaterialSeparationRecipes.IsReverseRecipe(recipe))
                return null;

            Dictionary<Tag, float> amounts = MaterialRecovery.CaptureIngredients(recipe, buildStorage);

            if (amounts.Count == 0)
                return null;

            return new CraftSnapshot
            {
                Ingredients = amounts,
                ProductTag = recipe.results[0].material,
                ProductAmount = recipe.results[0].amount
            };
        }
    }
}
