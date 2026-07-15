using System;
using System.Collections.Generic;
using UnityEngine;

namespace ONIUtilityTweaks.MaterialSeparator
{
    internal static class RecipeCatalog
    {
        private static readonly Dictionary<Tag, ComplexRecipe> recipesByProduct =
            new Dictionary<Tag, ComplexRecipe>();

        private static int cachedRecipeCount = -1;

        internal static bool IsReversible(ComplexRecipe recipe)
        {
            return recipe != null && !MaterialSeparationRecipes.IsReverseRecipe(recipe) &&
                !MaterialSeparationRecipes.IsRepairRecipe(recipe) &&
                recipe.ingredients != null && recipe.ingredients.Length > 0 &&
                recipe.results != null && recipe.results.Length > 0 &&
                recipe.results[0].amount > 0f;
        }

        internal static bool TryGetRecipe(Tag product, out ComplexRecipe recipe)
        {
            Refresh();
            return recipesByProduct.TryGetValue(product, out recipe);
        }

        internal static Tag ResolveSpawnableIngredient(ComplexRecipe.RecipeElement ingredient)
        {
            if (ingredient == null)
                return Tag.Invalid;

            if (CanSpawn(ingredient.material))
                return ingredient.material;

            if (ingredient.possibleMaterials != null)
                foreach (Tag possible in ingredient.possibleMaterials)
                    if (CanSpawn(possible))
                        return possible;

            List<GameObject> prefabs = Assets.GetPrefabsWithTag(ingredient.material);
            if (prefabs != null)
                foreach (GameObject prefab in prefabs)
                {
                    KPrefabID prefabId = prefab == null ? null : prefab.GetComponent<KPrefabID>();
                    if (prefabId != null && prefab.GetComponent<Pickupable>() != null)
                        return prefabId.PrefabTag;
                }

            return Tag.Invalid;
        }

        private static bool CanSpawn(Tag tag)
        {
            return tag.IsValid && (ElementLoader.FindElementByTag(tag) != null ||
                Assets.TryGetPrefab(tag) != null);
        }

        private static void Refresh(bool force = false)
        {
            ComplexRecipeManager manager = ComplexRecipeManager.Get();
            int count = manager == null || manager.recipes == null ? 0 : manager.recipes.Count;
            if (!force && count == cachedRecipeCount)
                return;

            cachedRecipeCount = count;
            recipesByProduct.Clear();
            if (manager == null || manager.recipes == null)
                return;

            foreach (ComplexRecipe recipe in manager.recipes)
            {
                if (!IsReversible(recipe))
                    continue;

                Tag product = recipe.results[0].material;
                GameObject prefab = Assets.TryGetPrefab(product);
                if (!product.IsValid || prefab == null || prefab.GetComponent<Pickupable>() == null)
                    continue;

                if (!recipesByProduct.ContainsKey(product))
                    recipesByProduct.Add(product, recipe);
            }
        }
    }
}
