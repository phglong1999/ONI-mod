using System;
using System.IO;
using Newtonsoft.Json;
using ONIUtilityTweaks.Support;
using UnityEngine;

namespace ONIUtilityTweaks.Settings
{
    internal static class NaturalConstructionLegacyImporter
    {
        internal static bool TryImport(ModSettingsData settings)
        {
            if (settings.NaturalConstructionSettingsImported)
                return false;

            string path = Path.Combine(ModPaths.OniDocuments, "mods", "config",
                "NaturalConstruction", "config.json");
            settings.NaturalConstructionSettingsImported = true;
            if (!File.Exists(path))
                return true;

            try
            {
                LegacyNaturalConstructionSettings legacy =
                    JsonConvert.DeserializeObject<LegacyNaturalConstructionSettings>(
                        File.ReadAllText(path));
                if (legacy != null)
                {
                    settings.EnableNaturalConstruction = true;
                    settings.ScaleNaturalConstructionTime =
                        legacy.ScalingConstructionTime;
                    settings.DefaultNaturalTileMass = legacy.DefaultMassTile;
                    settings.DefaultNaturalBackwallMass = legacy.DefaultMassBackwall;
                    settings.NaturalConstructionMassMultiplier =
                        legacy.SpawningMassMultiplier;
                    Debug.Log("[ONIUtilityTweaks] Imported Natural Construction " +
                        "settings from '" + path + "'.");
                }
            }
            catch (Exception ex)
            {
                settings.NaturalConstructionSettingsImported = false;
                Debug.LogWarning("[ONIUtilityTweaks] Could not import Natural " +
                    "Construction settings: " + ex.Message);
                return false;
            }
            return true;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class LegacyNaturalConstructionSettings
    {
        [JsonProperty("ScalingConstructionTime")]
        public bool ScalingConstructionTime { get; set; } = true;

        [JsonProperty("DefaultMass_Tile")]
        public int DefaultMassTile { get; set; } = 100;

        [JsonProperty("DefaultMass_Backwall")]
        public int DefaultMassBackwall { get; set; } = 100;

        [JsonProperty("SpawningMassMultiplier")]
        public float SpawningMassMultiplier { get; set; } = 1f;
    }
}
