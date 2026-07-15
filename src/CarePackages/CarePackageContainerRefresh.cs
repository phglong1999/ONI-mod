using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ONIUtilityTweaks.CarePackages
{
    internal static class CarePackageContainerRefresh
    {
        private static readonly FieldInfo entryIconsField = AccessTools.Field(
            typeof(CarePackageContainer), "entryIcons");

        private static readonly MethodInfo clearEntryIconsMethod = AccessTools.Method(
            typeof(CarePackageContainer), "ClearEntryIcons");

        private static readonly MethodInfo setAnimatorMethod = AccessTools.Method(
            typeof(CarePackageContainer), "SetAnimator");

        private static readonly MethodInfo setInfoTextMethod = AccessTools.Method(
            typeof(CarePackageContainer), "SetInfoText");

        private static bool warned;

        internal static void Refresh(CarePackageContainer container)
        {
            if (container == null || container.Info == null ||
                container.carePackageInstanceData == null)
                return;

            if (entryIconsField == null || clearEntryIconsMethod == null ||
                setAnimatorMethod == null || setInfoTextMethod == null)
            {
                Warn("required game methods were not found");
                return;
            }

            try
            {
                clearEntryIconsMethod.Invoke(container, null);
                var entryIcons = entryIconsField.GetValue(container) as List<GameObject>;
                entryIcons?.Clear();
                setAnimatorMethod.Invoke(container, null);
                setInfoTextMethod.Invoke(container, null);
            }
            catch (Exception ex)
            {
                Warn(ex.GetBaseException().Message);
            }
        }

        private static void Warn(string reason)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning("[ONIUtilityTweaks] Could not refresh a Care Package " +
                "container: " + reason);
        }
    }
}
