using System;
using System.Collections.Generic;
using UnityEngine;

namespace ONIUtilityTweaks.MaterialSeparator
{
    internal static class MaterialRecovery
    {
        internal const float MinimumAmount = 0.0001f;

        internal static Dictionary<Tag, float> CaptureIngredients(
            ComplexRecipe recipe,
            Storage buildStorage)
        {
            var captured = new Dictionary<Tag, float>();
            if (!RecipeCatalog.IsReversible(recipe))
                return captured;

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (!IsConsumedIngredient(ingredient))
                    continue;

                float remaining = ingredient.amount;
                if (buildStorage != null)
                    CaptureConcreteMaterials(buildStorage, ingredient, captured, ref remaining);

                if (remaining > MinimumAmount)
                {
                    Tag fallback = RecipeCatalog.ResolveSpawnableIngredient(ingredient);
                    if (fallback.IsValid)
                        Add(captured, fallback, remaining);
                }
            }

            return captured;
        }

        internal static bool TryGetYields(GameObject item, out Dictionary<Tag, float> yields)
        {
            yields = new Dictionary<Tag, float>();
            if (item == null)
                return false;

            CraftedRecipeOrigin origin = item.GetComponent<CraftedRecipeOrigin>();
            if (origin != null && origin.TryAddYields(item, yields))
                return yields.Count > 0;

            AddFallbackYields(item, yields);
            return yields.Count > 0;
        }

        internal static List<GameObject> SpawnAll(
            IDictionary<Tag, float> yields,
            float temperature,
            Vector3 position)
        {
            var spawned = new List<GameObject>();
            foreach (KeyValuePair<Tag, float> ingredient in yields)
            {
                GameObject item = Spawn(ingredient.Key, ingredient.Value, temperature, position);
                if (item != null)
                    spawned.Add(item);
            }
            return spawned;
        }

        internal static void Add(IDictionary<Tag, float> amounts, Tag tag, float amount)
        {
            if (!tag.IsValid || amount <= MinimumAmount)
                return;

            float current;
            amounts.TryGetValue(tag, out current);
            amounts[tag] = current + amount;
        }

        private static void AddFallbackYields(GameObject item, IDictionary<Tag, float> yields)
        {
            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            ComplexRecipe recipe;
            if (prefabId == null || !RecipeCatalog.TryGetRecipe(prefabId.PrefabTag, out recipe))
                return;

            float scale = GetProductScale(item, recipe.results[0].amount);
            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (!IsConsumedIngredient(ingredient))
                    continue;

                Tag tag = RecipeCatalog.ResolveSpawnableIngredient(ingredient);
                Add(yields, tag, ingredient.amount * scale);
            }
        }

        private static void CaptureConcreteMaterials(
            Storage buildStorage,
            ComplexRecipe.RecipeElement ingredient,
            IDictionary<Tag, float> captured,
            ref float remaining)
        {
            foreach (GameObject item in buildStorage.GetItems())
            {
                if (remaining <= MinimumAmount || !Matches(item, ingredient))
                    continue;

                KPrefabID prefabId = item.GetComponent<KPrefabID>();
                PrimaryElement element = item.GetComponent<PrimaryElement>();
                if (prefabId == null)
                    continue;

                float available = element == null ? remaining : Math.Max(0f, element.Mass);
                float used = Math.Min(remaining, available);
                Add(captured, prefabId.PrefabTag, used);
                remaining -= used;
            }
        }

        private static float GetProductScale(GameObject item, float recipeProductAmount)
        {
            PrimaryElement element = item.GetComponent<PrimaryElement>();
            float currentAmount = element == null
                ? recipeProductAmount
                : Math.Max(0f, element.Units);
            return currentAmount / Math.Max(MinimumAmount, recipeProductAmount);
        }

        private static bool IsConsumedIngredient(ComplexRecipe.RecipeElement ingredient)
        {
            return ingredient != null && !ingredient.doNotConsume && ingredient.amount > 0f;
        }

        private static bool Matches(GameObject item, ComplexRecipe.RecipeElement ingredient)
        {
            KPrefabID prefabId = item == null ? null : item.GetComponent<KPrefabID>();
            if (prefabId == null)
                return false;
            if (prefabId.HasTag(ingredient.material))
                return true;

            Tag[] possibilities = ingredient.possibleMaterials;
            if (possibilities != null)
                foreach (Tag possible in possibilities)
                    if (prefabId.HasTag(possible))
                        return true;
            return false;
        }

        private static GameObject Spawn(Tag tag, float amount, float temperature, Vector3 position)
        {
            if (amount <= MinimumAmount)
                return null;

            Element element = ElementLoader.FindElementByTag(tag);
            if (element != null && element.substance != null)
            {
                return element.substance.SpawnResource(position, amount, temperature,
                    Sim.InvalidDiseaseIdx, 0);
            }

            GameObject prefab = Assets.TryGetPrefab(tag);
            if (prefab == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Cannot spawn recovered ingredient: " + tag);
                return null;
            }

            GameObject spawned = Util.KInstantiate(prefab, position);
            PrimaryElement primaryElement = spawned.GetComponent<PrimaryElement>();
            if (primaryElement != null)
            {
                primaryElement.Temperature = temperature;
                primaryElement.Units = amount;
            }
            spawned.SetActive(true);
            return spawned;
        }
    }
}
