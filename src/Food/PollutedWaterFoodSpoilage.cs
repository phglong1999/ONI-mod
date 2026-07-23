using ONIUtilityTweaks.Settings;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace ONIUtilityTweaks.Food
{
    internal static class PollutedWaterFoodSpoilage
    {
        private static readonly int PollutedWaterId = (int)SimHashes.DirtyWater;

        private static bool enabled;
        private static bool snapshotCaptured;
        private static bool hadOriginalValue;
        private static Rottable.RotAtmosphereQuality originalValue;

        [PLibMethod(RunAt.AfterDbInit)]
        internal static void ApplyCurrentSettings()
        {
            Apply(ModSettings.Current.EnablePollutedWaterFoodSpoilage);
        }

        internal static void Apply(bool shouldEnable)
        {
            if (shouldEnable)
            {
                if (!snapshotCaptured)
                {
                    hadOriginalValue = Rottable.AtmosphereModifier.TryGetValue(
                        PollutedWaterId, out originalValue);
                    snapshotCaptured = true;
                }

                Rottable.AtmosphereModifier[PollutedWaterId] =
                    Rottable.RotAtmosphereQuality.Contaminating;
                enabled = true;
                return;
            }

            if (!enabled)
                return;

            if (Rottable.AtmosphereModifier.TryGetValue(
                    PollutedWaterId, out Rottable.RotAtmosphereQuality currentValue) &&
                currentValue == Rottable.RotAtmosphereQuality.Contaminating)
            {
                if (hadOriginalValue)
                    Rottable.AtmosphereModifier[PollutedWaterId] = originalValue;
                else
                    Rottable.AtmosphereModifier.Remove(PollutedWaterId);
            }

            enabled = false;
            snapshotCaptured = false;
            Debug.Log("[ONIUtilityTweaks] Restored the original polluted water food spoilage rule.");
        }
    }
}
