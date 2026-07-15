using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using TUNING;
using UnityEngine;

namespace ONIUtilityTweaks.CarePackages
{
    [HarmonyPatch(typeof(Immigration), nameof(Immigration.RandomCarePackage))]
    internal static class ImmigrationRandomCarePackagePatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(Immigration __instance)
        {
            CarePackageManager.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(SaveLoader), "OnSpawn")]
    internal static class CarePackageSaveLoaderPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Postfix()
        {
            CarePackageManager.Invalidate();
        }
    }

    [HarmonyPatch(typeof(CharacterSelectionController), "InitializeContainers")]
    internal static class CharacterSelectionInitializeContainersPatch
    {
        private static readonly FieldInfo duplicantOptionsField = AccessTools.Field(
            typeof(CharacterSelectionController), "numberOfDuplicantOptions");

        private static readonly FieldInfo carePackageOptionsField = AccessTools.Field(
            typeof(CharacterSelectionController), "numberOfCarePackageOptions");

        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            FieldInfo duplicantCount = AccessTools.Field(
                typeof(CharacterSelectionController), "numberOfDuplicantOptions");
            MethodInfo applyCounts = AccessTools.Method(
                typeof(CharacterSelectionInitializeContainersPatch), nameof(ApplyCounts));
            MethodInfo setSiblingIndex = AccessTools.Method(
                typeof(Transform), nameof(Transform.SetSiblingIndex));
            MethodInfo setSiblingIfUnordered = AccessTools.Method(
                typeof(CharacterSelectionInitializeContainersPatch),
                nameof(SetSiblingIndexIfUnordered));

            int insertAt = -1;
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Stfld &&
                    Equals(codes[i].operand, duplicantCount))
                {
                    insertAt = i + 1;
                    break;
                }
            }

            if (insertAt >= 0 && applyCounts != null)
            {
                var loadController = new CodeInstruction(OpCodes.Ldarg_0);
                if (insertAt < codes.Count && codes[insertAt].labels.Count > 0)
                {
                    loadController.labels.AddRange(codes[insertAt].labels);
                    codes[insertAt].labels.Clear();
                }
                codes.Insert(insertAt, loadController);
                codes.Insert(insertAt + 1,
                    new CodeInstruction(OpCodes.Call, applyCounts));
            }
            else
            {
                Debug.LogWarning("[ONIUtilityTweaks] Could not patch Printing Pod roster counts.");
            }

            if (setSiblingIndex != null && setSiblingIfUnordered != null)
            {
                foreach (CodeInstruction code in codes)
                    if (code.Calls(setSiblingIndex))
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = setSiblingIfUnordered;
                    }
            }
            return codes;
        }

        private static void ApplyCounts(CharacterSelectionController controller)
        {
            ModSettingsData settings = ModSettings.Current;
            if (controller == null || controller.IsStarterMinion ||
                !settings.UseCustomCarePackageRoster)
                return;

            duplicantOptionsField?.SetValue(controller,
                settings.CarePackageRosterDuplicants);
            carePackageOptionsField?.SetValue(controller,
                settings.CarePackageRosterPackages);
        }

        private static void SetSiblingIndexIfUnordered(Transform transform, int index)
        {
            if (!ModSettings.Current.OrderCarePackageRoster ||
                !DlcManager.IsExpansion1Active())
                transform.SetSiblingIndex(index);
        }
    }

    [HarmonyPatch(typeof(CharacterContainer), nameof(CharacterContainer.SetReshufflingState))]
    internal static class CharacterContainerReshuffleStatePatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(ref bool enable)
        {
            if (ModSettings.Current.AllowCarePackageReshuffle)
                enable = true;
        }
    }

    [HarmonyPatch(typeof(CharacterContainer), nameof(CharacterContainer.GenerateCharacter))]
    internal static class CharacterContainerGenerateCharacterPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(ref bool is_starter)
        {
            if (ModSettings.Current.RemoveStarterDuplicantRestrictions)
                is_starter = false;
        }
    }

    [HarmonyPatch(typeof(CharacterSelectionController), nameof(CharacterSelectionController.AddDeliverable))]
    internal static class CharacterSelectionAddDeliverablePatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(CharacterSelectionController __instance)
        {
            if (ModSettings.Current.AllowCarePackageReshuffle)
                __instance.RemoveLast();
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateTraits")]
    internal static class MinionGenerateTraitsPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Postfix(ref int __result, MinionStartingStats __instance)
        {
            if (__instance.personality.model != BionicMinionConfig.MODEL)
                __result += ModSettings.Current.CarePackageAttributeBonusChance;
        }
    }

    [HarmonyPatch(typeof(MinionStartingStats), "GenerateAptitudes")]
    internal static class MinionGenerateAptitudesPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo randomRange = AccessTools.Method(typeof(UnityEngine.Random),
                nameof(UnityEngine.Random.Range), new[] { typeof(int), typeof(int) });
            MethodInfo replacement = AccessTools.Method(
                typeof(MinionGenerateAptitudesPatch), nameof(RandomInterestCount));
            bool replaced = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!replaced && randomRange != null && instruction.Calls(randomRange))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced = true;
                }
                yield return instruction;
            }

            if (!replaced)
                Debug.LogWarning("[ONIUtilityTweaks] Could not patch Duplicant interest count.");
        }

        private static int RandomInterestCount(int minInclusive, int maxExclusive)
        {
            int min = Math.Max(1, ModSettings.Current.MinimumDuplicantInterests);
            int max = Math.Max(min, Math.Min(
                ModSettings.Current.MaximumDuplicantInterests,
                DUPLICANTSTATS.APTITUDE_ATTRIBUTE_BONUSES.Length));
            return UnityEngine.Random.Range(min, max + 1);
        }
    }

    [HarmonyPatch(typeof(HeadquartersConfig), nameof(HeadquartersConfig.ConfigureBuildingTemplate))]
    internal static class HeadquartersCarePackageOptionsPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Postfix(GameObject go)
        {
            go.AddOrGet<CarePackageOptionsButton>();
        }
    }
}
