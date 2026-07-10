using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ONIUtilityTweaks.ScheduleSync;
using PeterHan.PLib.Options;
using UnityEngine;

namespace ONIUtilityTweaks.Settings
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("settings.json", IndentOutput: true, SharedConfigLocation: true)]
    public sealed class ModSettingsData : IOptions
    {
        [JsonProperty]
        [Option("Saved Schedules", "Show the Saved button in Schedule Editor.")]
        public bool EnableScheduleTemplates { get; set; }

        [JsonProperty]
        [Option("Unlock Biome Remix Limit", "Allow selecting all Biome Remix entries when creating a new game.")]
        public bool UnlockBiomeRemixLimit { get; set; }

        public ModSettingsData()
        {
            EnableScheduleTemplates = true;
            UnlockBiomeRemixLimit = true;
        }

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return null;
        }

        public void OnOptionsChanged()
        {
            ModSettings.ApplyChangedSettings(this);
        }
    }

    internal static class ModSettings
    {
        private static ModSettingsData current;

        public static ModSettingsData Current
        {
            get
            {
                if (current == null)
                    Reload();

                return current;
            }
        }

        public static string SettingsPath => POptions.GetConfigFilePath(typeof(ModSettingsData));

        public static void Reload()
        {
            current = LoadFromDisk();
        }

        public static void ApplyChangedSettings(ModSettingsData settings)
        {
            current = Normalize(settings);
            ScheduleTemplatePanel.Close();
            ScheduleScreenTemplateButtonPatch.ApplyCurrentSettings();
        }

        private static ModSettingsData LoadFromDisk()
        {
            try
            {
                return Normalize(POptions.ReadSettings<ModSettingsData>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ONIUtilityTweaks] Could not read settings '{SettingsPath}': {ex.Message}");
            }

            return new ModSettingsData();
        }

        private static ModSettingsData Normalize(ModSettingsData settings)
        {
            return settings ?? new ModSettingsData();
        }
    }
}
