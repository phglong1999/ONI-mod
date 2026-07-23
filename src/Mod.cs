using HarmonyLib;
using KMod;
using ONIUtilityTweaks.CarePackages;
using ONIUtilityTweaks.Doors;
using ONIUtilityTweaks.Food;
using ONIUtilityTweaks.NaturalConstruction;
using ONIUtilityTweaks.Settings;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace ONIUtilityTweaks
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            Localization.RegisterForTranslation(typeof(NaturalConstruction.STRINGS));
            LocString.CreateLocStringKeys(typeof(NaturalConstruction.STRINGS), null);
            var patchManager = new PPatchManager(harmony);
            patchManager.RegisterPatchClass(typeof(GasBlockingDoorPatchBootstrap));
            patchManager.RegisterPatchClass(typeof(PollutedWaterFoodSpoilage));
            new POptions().RegisterOptions(this, typeof(ModSettingsData));
            ModSettings.Reload();
            if (CarePackageManager.ExternalModLoaded)
                Debug.LogWarning("[ONIUtilityTweaks] Care Package Mod is also enabled; " +
                    "the integrated care package patches were skipped to prevent conflicts.");
            Debug.Log("[ONIUtilityTweaks] Loaded. Utility settings are enabled.");
        }
    }
}
