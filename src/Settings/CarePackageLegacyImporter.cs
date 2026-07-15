using System;
using System.IO;
using Newtonsoft.Json;
using ONIUtilityTweaks.CarePackages;
using ONIUtilityTweaks.Support;
using UnityEngine;

namespace ONIUtilityTweaks.Settings
{
    internal static class CarePackageLegacyImporter
    {
        private const string LegacyFileName = "Care Package Manager.json";

        internal static bool TryImport(ModSettingsData settings)
        {
            if (settings.CarePackageSettingsImported)
                return false;

            string path = Path.Combine(ModPaths.OniDocuments, "mods", LegacyFileName);
            if (!File.Exists(path))
            {
                settings.CarePackageSettingsImported = true;
                return true;
            }

            try
            {
                LegacyCarePackageSettings legacy = JsonConvert.DeserializeObject<
                    LegacyCarePackageSettings>(File.ReadAllText(path));
                if (legacy == null)
                {
                    settings.CarePackageSettingsImported = true;
                    return true;
                }

                settings.UseCustomCarePackageRoster = legacy.BiggerRoster;
                settings.CarePackageRosterDuplicants = legacy.RosterDupes;
                settings.CarePackageRosterPackages = legacy.RosterPackages;
                settings.CarePackageAttributeBonusChance = legacy.AttributeBonusChance;
                settings.MinimumDuplicantInterests = legacy.MinimumInterests;
                settings.MaximumDuplicantInterests = legacy.MaximumInterests;
                settings.RemoveStarterDuplicantRestrictions = legacy.RemoveStarterRestriction;
                settings.AllowCarePackageReshuffle = legacy.AllowReshuffle;
                settings.OrderCarePackageRoster = legacy.RosterIsOrdered;
                settings.CarePackageMultiplier = Mathf.Clamp(
                    Mathf.RoundToInt(legacy.Multiplier),
                    ModSettingsData.MinCarePackageMultiplier,
                    ModSettingsData.MaxCarePackageMultiplier);
                settings.LoadCustomCarePackages = legacy.LoadPackages;
                if (legacy.CarePackages != null && legacy.CarePackages.Length > 0)
                {
                    settings.CarePackages = legacy.CarePackages;
                    settings.CarePackageDefaultsVersion = 0;
                }
                settings.CarePackageSettingsImported = true;

                Debug.Log("[ONIUtilityTweaks] Imported Care Package Mod settings from '" +
                    path + "'.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Could not import Care Package Mod " +
                    "settings: " + ex.Message);
                return false;
            }
            return true;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class LegacyCarePackageSettings
    {
        [JsonProperty("biggerRoster")]
        public bool BiggerRoster { get; set; } = true;

        [JsonProperty("rosterDupes")]
        public int RosterDupes { get; set; } = 3;

        [JsonProperty("rosterPackages")]
        public int RosterPackages { get; set; } = 3;

        [JsonProperty("attributeBonusChance")]
        public int AttributeBonusChance { get; set; }

        [JsonProperty("minNumberofInterests")]
        public int MinimumInterests { get; set; } = 1;

        [JsonProperty("maxNumberofInterests")]
        public int MaximumInterests { get; set; } = 3;

        [JsonProperty("removeStarterRestriction")]
        public bool RemoveStarterRestriction { get; set; } = true;

        [JsonProperty("allowReshuffle")]
        public bool AllowReshuffle { get; set; }

        [JsonProperty("rosterIsOrdered")]
        public bool RosterIsOrdered { get; set; }

        [JsonProperty("multiplier")]
        public float Multiplier { get; set; } = 1f;

        [JsonProperty("loadPackages")]
        public bool LoadPackages { get; set; } = true;

        [JsonProperty("CarePackages")]
        public CarePackageDefinition[] CarePackages { get; set; }
    }
}
