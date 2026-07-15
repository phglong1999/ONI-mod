using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.CarePackages
{
    internal static class CarePackageManager
    {
        private static List<CarePackageInfo> cachedPackages;
        private static bool dirty = true;
        private static Immigration trackedImmigration;
        private static List<CarePackageInfo> vanillaPackages;
        private static bool customPackagesApplied;

        private static readonly FieldInfo carePackagesField =
            AccessTools.Field(typeof(Immigration), "carePackages");

        internal static ModSettingsData Settings => ModSettings.Current;

        internal static bool ExternalModLoaded
        {
            get
            {
                return AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                    string.Equals(assembly.GetName().Name, "CarePackageMod",
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        internal static void Apply(Immigration immigration)
        {
            if (ExternalModLoaded || immigration == null)
                return;

            TrackImmigration(immigration);
            if (!Settings.LoadCustomCarePackages)
            {
                RestoreVanillaPackages(immigration);
                return;
            }

            if (cachedPackages == null)
                cachedPackages = BuildPackages();
            if (dirty && SetPackages(immigration, cachedPackages))
            {
                customPackagesApplied = true;
                dirty = false;
            }
        }

        internal static CarePackageInfo[] GetPackages()
        {
            if (Immigration.Instance != null)
            {
                var active = carePackagesField?.GetValue(Immigration.Instance) as
                    IEnumerable<CarePackageInfo>;
                if (active != null)
                    return active.ToArray();
            }
            if (cachedPackages == null)
                cachedPackages = BuildPackages();
            return cachedPackages.ToArray();
        }

        internal static void Invalidate()
        {
            cachedPackages = null;
            dirty = true;
        }

        private static void TrackImmigration(Immigration immigration)
        {
            if (ReferenceEquals(trackedImmigration, immigration))
                return;

            trackedImmigration = immigration;
            vanillaPackages = ReadPackages(immigration);
            customPackagesApplied = false;
            dirty = true;
        }

        private static void RestoreVanillaPackages(Immigration immigration)
        {
            if (!customPackagesApplied)
                return;

            if (SetPackages(immigration, vanillaPackages))
            {
                customPackagesApplied = false;
                dirty = true;
            }
        }

        private static List<CarePackageInfo> ReadPackages(Immigration immigration)
        {
            var packages = carePackagesField?.GetValue(immigration) as
                IEnumerable<CarePackageInfo>;
            return packages == null
                ? new List<CarePackageInfo>()
                : new List<CarePackageInfo>(packages);
        }

        private static bool SetPackages(
            Immigration immigration,
            IEnumerable<CarePackageInfo> packages)
        {
            if (carePackagesField == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Could not find Immigration.carePackages.");
                return false;
            }

            try
            {
                carePackagesField.SetValue(immigration,
                    packages == null
                        ? new List<CarePackageInfo>()
                        : new List<CarePackageInfo>(packages));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Could not update care packages: " +
                    ex.Message);
                return false;
            }
        }

        internal static void OverridePackages(IEnumerable<CarePackageInfo> packages)
        {
            cachedPackages = packages == null
                ? new List<CarePackageInfo>()
                : new List<CarePackageInfo>(packages);
            dirty = true;
            if (Immigration.Instance != null && Settings.LoadCustomCarePackages)
                Apply(Immigration.Instance);
        }

        private static List<CarePackageInfo> BuildPackages()
        {
            var result = new List<CarePackageInfo>();
            CarePackageDefinition[] definitions = Settings.CarePackages;
            if (definitions == null)
                return result;

            foreach (CarePackageDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                    continue;

                GameObject prefab = Assets.TryGetPrefab(definition.Id.ToTag());
                if (prefab == null)
                {
                    Debug.LogWarning("[ONIUtilityTweaks] Ignoring invalid care package prefab: " +
                        definition.Id);
                    continue;
                }
                result.Add(definition.ToInfo(Settings.CarePackageMultiplier));
            }

            Debug.Log("[ONIUtilityTweaks] Loaded " + result.Count +
                " configured care packages.");
            return result;
        }
    }

    public static class CarePackageApi
    {
        public static bool Reload()
        {
            CarePackageManager.Invalidate();
            if (Immigration.Instance != null)
                CarePackageManager.Apply(Immigration.Instance);
            return true;
        }

        public static bool OverridePackages(
            IEnumerable<CarePackageDefinition> definitions)
        {
            if (definitions == null)
                return false;

            var packages = new List<CarePackageInfo>();
            foreach (CarePackageDefinition definition in definitions)
                if (definition != null)
                    packages.Add(definition.ToInfo(
                        CarePackageManager.Settings.CarePackageMultiplier));
            CarePackageManager.OverridePackages(packages);
            return true;
        }

        public static void OverridePackages(IEnumerable<CarePackageInfo> packages)
        {
            CarePackageManager.OverridePackages(packages);
        }

        public static CarePackageInfo[] GetPackages()
        {
            return CarePackageManager.GetPackages();
        }
    }
}
