using HarmonyLib;
using KMod;
using ONIUtilityTweaks.Settings;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

namespace ONIUtilityTweaks
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModSettingsData));
            ModSettings.Reload();
            Debug.Log("[ONIUtilityTweaks] Loaded. Utility settings are enabled.");
        }
    }
}
