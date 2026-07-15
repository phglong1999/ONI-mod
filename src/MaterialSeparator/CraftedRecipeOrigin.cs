using System;
using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace ONIUtilityTweaks.MaterialSeparator
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class CraftedRecipeOrigin : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        [SerializeField]
        private string[] IngredientTags;

        [Serialize]
        [SerializeField]
        private float[] IngredientAmounts;

        [Serialize]
        [SerializeField]
        private float ProductAmount;

        internal void Record(
            float productAmount,
            IDictionary<Tag, float> ingredients)
        {
            ProductAmount = Math.Max(MaterialRecovery.MinimumAmount, productAmount);
            IngredientTags = new string[ingredients.Count];
            IngredientAmounts = new float[ingredients.Count];

            int index = 0;
            foreach (KeyValuePair<Tag, float> ingredient in ingredients)
            {
                IngredientTags[index] = ingredient.Key.Name;
                IngredientAmounts[index] = ingredient.Value;
                index++;
            }
        }

        internal bool TryAddYields(GameObject item, IDictionary<Tag, float> yields)
        {
            if (IngredientTags == null || IngredientAmounts == null || ProductAmount <= 0f)
                return false;

            PrimaryElement element = item.GetComponent<PrimaryElement>();
            float currentAmount = element == null ? ProductAmount : Math.Max(0f, element.Units);
            float scale = currentAmount / Math.Max(MaterialRecovery.MinimumAmount, ProductAmount);
            int count = Math.Min(IngredientTags.Length, IngredientAmounts.Length);
            int previousCount = yields.Count;
            for (int i = 0; i < count; i++)
                MaterialRecovery.Add(yields, new Tag(IngredientTags[i]), IngredientAmounts[i] * scale);
            return yields.Count > previousCount;
        }
    }
}
