using System;
using System.Collections.Generic;
using Database;
using HarmonyLib;
using ONIUtilityTweaks.Settings;

namespace ONIUtilityTweaks.CarePackages
{
    internal static class PrimoGarbFacadeSelector
    {
        internal static string SelectPreferredFacade(
            ISet<string> excludedFacades = null)
        {
            var available = new List<string>();
            foreach (EquippableFacadeResource facade in
                Db.GetEquippableFacades().resources)
            {
                if (facade != null && CarePackageDefaults.IsPrimoGarb(facade.DefID))
                {
                    available.Add(facade.Id);
                }
            }

            if (available.Count == 0)
                return null;

            var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Assignable assignable in Components.AssignableItems.Items)
            {
                Equippable equippable = assignable as Equippable;
                EquippableFacade facade = equippable == null
                    ? null
                    : equippable.GetComponent<EquippableFacade>();
                if (facade != null && !string.IsNullOrWhiteSpace(facade.FacadeID))
                    owned.Add(facade.FacadeID);
            }

            var missing = available.FindAll(facadeId =>
                !owned.Contains(facadeId) && !IsExcluded(facadeId, excludedFacades));
            var unused = available.FindAll(facadeId =>
                !IsExcluded(facadeId, excludedFacades));
            List<string> candidates = missing.Count > 0
                ? missing
                : unused.Count > 0 ? unused : available;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static bool IsExcluded(string facadeId, ISet<string> excludedFacades)
        {
            return excludedFacades != null && excludedFacades.Contains(facadeId);
        }
    }

    [HarmonyPatch(typeof(CarePackageContainer), "GenerateCharacter")]
    internal static class CarePackageContainerPrimoGarbFacadePatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Postfix(CarePackageContainer __instance)
        {
            if (!ModSettings.Current.LoadCustomCarePackages || __instance == null)
                return;

            CarePackageInfo info = __instance.Info;
            CarePackageContainer.CarePackageInstanceData instanceData =
                __instance.carePackageInstanceData;
            if (info == null || instanceData == null ||
                !CarePackageDefaults.IsPrimoGarb(info.id) ||
                info.facadeID != Immigration.FACADE_SELECT_RANDOM)
            {
                return;
            }

            string facadeId = PrimoGarbFacadeSelector.SelectPreferredFacade();
            if (string.IsNullOrEmpty(facadeId) ||
                string.Equals(instanceData.facadeID, facadeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            instanceData.facadeID = facadeId;
            CarePackageContainerRefresh.Refresh(__instance);
        }
    }

    internal static class PrimoGarbPackageContents
    {
        [ThreadStatic]
        private static HashSet<string> usedFacades;

        internal static void Begin(CarePackage package)
        {
            usedFacades = package?.info != null &&
                package.info.quantity > 1f &&
                CarePackageDefaults.IsPrimoGarb(package.info.id)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : null;
        }

        internal static void ApplyDistinctFacade(ref string facadeId)
        {
            if (usedFacades == null)
                return;

            if (usedFacades.Count > 0 || string.IsNullOrWhiteSpace(facadeId))
                facadeId = PrimoGarbFacadeSelector.SelectPreferredFacade(usedFacades);

            if (!string.IsNullOrWhiteSpace(facadeId))
                usedFacades.Add(facadeId);
        }

        internal static void End()
        {
            usedFacades = null;
        }
    }

    [HarmonyPatch(typeof(CarePackage), "SpawnContents")]
    internal static class CarePackagePrimoGarbContentsPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(CarePackage __instance)
        {
            PrimoGarbPackageContents.Begin(__instance);
        }

        private static Exception Finalizer(Exception __exception)
        {
            PrimoGarbPackageContents.End();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(EquippableFacade),
        nameof(EquippableFacade.AddFacadeToEquippable))]
    internal static class EquippableFacadePrimoGarbContentsPatch
    {
        private static bool Prepare()
        {
            return !CarePackageManager.ExternalModLoaded;
        }

        private static void Prefix(ref string facadeID)
        {
            PrimoGarbPackageContents.ApplyDistinctFacade(ref facadeID);
        }
    }
}
