using HarmonyLib;
using Klei.AI;
using ONIUtilityTweaks.Settings;

namespace ONIUtilityTweaks.Food
{
    internal static class FoodSpoilageMultiplier
    {
        private static AttributeModifier multiplierModifier;

        internal static void Apply(
            Rottable.Instance rottable,
            AmountInstance rotAmountInstance)
        {
            if (!IsPollutedEnvironment(rottable) || rotAmountInstance == null ||
                rotAmountInstance.deltaAttribute.GetTotalValue() >= 0f)
                return;

            int multiplier = ModSettings.Current.FoodSpoilageMultiplier;
            if (multiplier <= ModSettingsData.MinFoodSpoilageMultiplier)
                return;

            if (multiplierModifier == null)
            {
                multiplierModifier = new AttributeModifier(
                    rotAmountInstance.amount.Id,
                    1f - multiplier,
                    "Polluted Environment Spoilage Multiplier",
                    is_multiplier: true);
            }
            else
            {
                multiplierModifier.SetValue(1f - multiplier);
            }

            rotAmountInstance.deltaAttribute.Add(multiplierModifier);
        }

        private static bool IsPollutedEnvironment(Rottable.Instance rottable)
        {
            if (rottable == null || rottable.gameObject == null)
                return false;

            int cell = Grid.PosToCell(rottable.gameObject);
            if (!Grid.IsValidCell(cell))
                return false;

            return IsPollutedElement(cell) || IsPollutedElement(Grid.CellAbove(cell));
        }

        private static bool IsPollutedElement(int cell)
        {
            if (!Grid.IsValidCell(cell))
                return false;

            SimHashes element = Grid.Element[cell].id;
            return element == SimHashes.ContaminatedOxygen ||
                element == SimHashes.DirtyWater;
        }
    }

    [HarmonyPatch(typeof(Rottable.Instance), nameof(Rottable.Instance.RefreshModifiers))]
    internal static class FoodSpoilageMultiplierPatch
    {
        internal static void Postfix(
            Rottable.Instance __instance,
            AmountInstance ___rotAmountInstance)
        {
            FoodSpoilageMultiplier.Apply(__instance, ___rotAmountInstance);
        }
    }
}
