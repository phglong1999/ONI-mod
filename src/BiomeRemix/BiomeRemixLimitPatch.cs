using System.Reflection;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.BiomeRemix
{
    internal static class BiomeRemixLimitPatch
    {
        internal const int UnlockedLimit = 999;

        internal static MethodBase GetMethod(string typeName, string methodName)
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                Debug.LogWarning($"[ONIUtilityTweaks] Could not find type {typeName}; Biome Remix limit patch skipped.");
                return null;
            }

            var method = AccessTools.Method(type, methodName);
            if (method == null)
                Debug.LogWarning($"[ONIUtilityTweaks] Could not find method {typeName}.{methodName}; Biome Remix limit patch skipped.");

            return method;
        }
    }

    [HarmonyPatch]
    internal static class BiomeRemixMaxWorldMixingsPatch
    {
        public static MethodBase TargetMethod()
        {
            return BiomeRemixLimitPatch.GetMethod("MixingContentPanel", "GetMaxNumOfGuaranteedWorldMixings");
        }

        public static void Postfix(ref int __result)
        {
            if (ModSettings.Current.UnlockBiomeRemixLimit)
                __result = BiomeRemixLimitPatch.UnlockedLimit;
        }
    }

    [HarmonyPatch]
    internal static class BiomeRemixMaxSubworldMixingsPatch
    {
        public static MethodBase TargetMethod()
        {
            return BiomeRemixLimitPatch.GetMethod("MixingContentPanel", "GetMaxNumOfGuaranteedSubworldMixings");
        }

        public static void Postfix(ref int __result)
        {
            if (ModSettings.Current.UnlockBiomeRemixLimit)
                __result = BiomeRemixLimitPatch.UnlockedLimit;
        }
    }

    [HarmonyPatch]
    internal static class BiomeRemixValidateWorldMixingOptionsPatch
    {
        public static MethodBase TargetMethod()
        {
            return BiomeRemixLimitPatch.GetMethod("ProcGenGame.WorldgenMixing", "ValidateWorldMixingOptions");
        }

        public static bool Prefix(ref bool __result)
        {
            if (!ModSettings.Current.UnlockBiomeRemixLimit)
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class BiomeRemixValidateSubworldMixingOptionsPatch
    {
        public static MethodBase TargetMethod()
        {
            return BiomeRemixLimitPatch.GetMethod("ProcGenGame.WorldgenMixing", "ValidateSubworldMixingOptions");
        }

        public static bool Prefix()
        {
            return !ModSettings.Current.UnlockBiomeRemixLimit;
        }
    }
}
