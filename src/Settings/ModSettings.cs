using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ONIUtilityTweaks.CarePackages;
using ONIUtilityTweaks.Doors;
using ONIUtilityTweaks.ScheduleSync;
using PeterHan.PLib.Options;
using UnityEngine;

namespace ONIUtilityTweaks.Settings
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("settings.json", IndentOutput: true, SharedConfigLocation: true)]
    public sealed class ModSettingsData : IOptions
    {
        internal const int MinCarePackageMultiplier = 1;
        internal const int MaxCarePackageMultiplier = 100;

        [JsonProperty]
        [Option("Saved Schedules", "Show the Saved button in Schedule Editor.")]
        public bool EnableScheduleTemplates { get; set; }

        [JsonProperty]
        [Option("Unlock Biome Remix Limit", "Allow selecting all Biome Remix entries when creating a new game.")]
        public bool UnlockBiomeRemixLimit { get; set; }

        [JsonProperty]
        [Option("Custom Printing Pod Roster", "Use configured counts for Duplicants and care packages shown by the Printing Pod.", "Care Packages")]
        public bool UseCustomCarePackageRoster { get; set; }

        [JsonProperty]
        [Option("Duplicant Choices", "Number of Duplicants shown in the Printing Pod roster.", "Care Packages")]
        [Limit(0, 6)]
        public int CarePackageRosterDuplicants { get; set; }

        [JsonProperty]
        [Option("Care Package Choices", "Number of care packages shown in the Printing Pod roster.", "Care Packages")]
        [Limit(0, 6)]
        public int CarePackageRosterPackages { get; set; }

        [JsonProperty]
        [Option("Attribute Bonus Chance", "Positive values increase generated Duplicant attributes; negative values reduce them.", "Care Packages")]
        public int CarePackageAttributeBonusChance { get; set; }

        [JsonProperty]
        [Option("Minimum Interests", "Minimum number of interests generated for a new Duplicant.", "Care Packages")]
        [Limit(1, 3)]
        public int MinimumDuplicantInterests { get; set; }

        [JsonProperty]
        [Option("Maximum Interests", "Maximum number of interests generated for a new Duplicant.", "Care Packages")]
        [Limit(1, 3)]
        public int MaximumDuplicantInterests { get; set; }

        [JsonProperty]
        [Option("Remove Starter Restrictions", "Allow starter Duplicants to use the broader trait and personality generation rules.", "Care Packages")]
        public bool RemoveStarterDuplicantRestrictions { get; set; }

        [JsonProperty]
        [Option("Allow Printing Pod Reshuffle", "Show and enable reroll controls for Printing Pod Duplicants.", "Care Packages")]
        public bool AllowCarePackageReshuffle { get; set; }

        [JsonProperty]
        [Option("Order Roster By Type", "Keep Duplicants and care packages grouped by type in Spaced Out.", "Care Packages")]
        public bool OrderCarePackageRoster { get; set; }

        [JsonProperty]
        [Option("Care Package Quantity Multiplier", "Multiply all configured Care Packages, including creatures and eggs, by this value.", "Care Packages")]
        [Limit(MinCarePackageMultiplier, MaxCarePackageMultiplier)]
        public int CarePackageMultiplier { get; set; }

        [JsonProperty("DoublePrintingPodItems", NullValueHandling = NullValueHandling.Ignore)]
        public bool? LegacyDoublePrintingPodItems { get; set; }

        [JsonProperty]
        [Option("Load Custom Care Packages", "Replace the vanilla care package pool with the list stored in settings.json.", "Care Packages")]
        public bool LoadCustomCarePackages { get; set; }

        [JsonProperty]
        public CarePackageDefinition[] CarePackages { get; set; }

        [JsonProperty]
        public bool CarePackageSettingsImported { get; set; }

        [JsonProperty]
        public int CarePackageDefaultsVersion { get; set; }

        [JsonProperty]
        [Option("Crafting Station Material Separation", "Add recipes to the Crafting Station that recover ingredients from crafted items.")]
        [RestartRequired]
        public bool EnableMaterialSeparator { get; set; }

        [JsonProperty]
        [Option("Gas-Sealing Airlocks", "Keep manual and mechanized airlocks sealed against gas and liquid while allowing duplicants to pass when open.")]
        public bool EnableGasBlockingDoors { get; set; }

        [JsonProperty]
        [Option("Natural Construction", "Build natural tiles and backwalls from any solid material.", "Natural Construction")]
        [RestartRequired]
        public bool EnableNaturalConstruction { get; set; }

        [JsonProperty]
        [Option("Scale Construction Time", "Scale construction time with the selected mass, from 5 to 100 seconds.", "Natural Construction")]
        [RestartRequired]
        public bool ScaleNaturalConstructionTime { get; set; }

        [JsonProperty]
        [Option("Default Natural Tile Mass", "Default material mass for a Natural Tile. It can still be changed on each blueprint.", "Natural Construction")]
        [Limit(1, 2000)]
        [RestartRequired]
        public int DefaultNaturalTileMass { get; set; }

        [JsonProperty]
        [Option("Default Natural Backwall Mass", "Default material mass for a Natural Backwall. It can still be changed on each blueprint.", "Natural Construction")]
        [Limit(1, 2000)]
        [RestartRequired]
        public int DefaultNaturalBackwallMass { get; set; }

        [JsonProperty]
        [Option("Spawn Mass Multiplier", "Multiply the selected construction mass in the completed natural tile or backwall.", "Natural Construction")]
        [Limit(1f, 2f)]
        [RestartRequired]
        public float NaturalConstructionMassMultiplier { get; set; }

        [JsonProperty]
        public bool NaturalConstructionSettingsImported { get; set; }

        public ModSettingsData()
        {
            EnableScheduleTemplates = true;
            UnlockBiomeRemixLimit = false;
            UseCustomCarePackageRoster = false;
            CarePackageRosterDuplicants = 3;
            CarePackageRosterPackages = 3;
            CarePackageAttributeBonusChance = 0;
            MinimumDuplicantInterests = 1;
            MaximumDuplicantInterests = 3;
            RemoveStarterDuplicantRestrictions = false;
            AllowCarePackageReshuffle = false;
            OrderCarePackageRoster = false;
            CarePackageMultiplier = 1;
            LegacyDoublePrintingPodItems = null;
            LoadCustomCarePackages = false;
            CarePackages = CarePackageDefaults.Create();
            CarePackageSettingsImported = false;
            CarePackageDefaultsVersion = 0;
            EnableMaterialSeparator = false;
            EnableGasBlockingDoors = false;
            EnableNaturalConstruction = false;
            ScaleNaturalConstructionTime = true;
            DefaultNaturalTileMass = 100;
            DefaultNaturalBackwallMass = 100;
            NaturalConstructionMassMultiplier = 1f;
            NaturalConstructionSettingsImported = false;
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
        private const int CurrentCarePackageSettingsVersion = 5;

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
            ModSettingsData normalized = Normalize(settings);
            bool gasBlockingDoorsChanged = Current.EnableGasBlockingDoors !=
                normalized.EnableGasBlockingDoors;
            current = normalized;
            ScheduleTemplatePanel.Close();
            ScheduleScreenTemplateButtonPatch.ApplyCurrentSettings();
            CarePackageManager.Invalidate();
            if (Immigration.Instance != null)
                CarePackageManager.Apply(Immigration.Instance);
            if (gasBlockingDoorsChanged)
                GasBlockingDoors.RefreshAll();
        }

        private static ModSettingsData LoadFromDisk()
        {
            try
            {
                ModSettingsData settings = POptions.ReadSettings<ModSettingsData>();
                int previousSettingsVersion = settings?.CarePackageDefaultsVersion ?? 0;
                settings = Normalize(settings);
                bool settingsUpgraded = previousSettingsVersion !=
                    settings.CarePackageDefaultsVersion;
                bool legacyImported = CarePackageLegacyImporter.TryImport(settings);
                bool naturalConstructionImported =
                    NaturalConstructionLegacyImporter.TryImport(settings);
                if (legacyImported || naturalConstructionImported)
                    settings = Normalize(settings);
                if (legacyImported || naturalConstructionImported || settingsUpgraded)
                    POptions.WriteSettings(settings);
                return settings;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ONIUtilityTweaks] Could not read settings '{SettingsPath}': {ex.Message}");
            }

            return new ModSettingsData();
        }

        private static ModSettingsData Normalize(ModSettingsData settings)
        {
            settings = settings ?? new ModSettingsData();
            settings.CarePackageRosterDuplicants = Mathf.Clamp(
                settings.CarePackageRosterDuplicants, 0, 6);
            settings.CarePackageRosterPackages = Mathf.Clamp(
                settings.CarePackageRosterPackages, 0, 6);
            settings.MinimumDuplicantInterests = Mathf.Clamp(
                settings.MinimumDuplicantInterests, 1, 3);
            settings.MaximumDuplicantInterests = Mathf.Clamp(
                settings.MaximumDuplicantInterests,
                settings.MinimumDuplicantInterests, 3);
            settings.CarePackageMultiplier = Mathf.Clamp(
                settings.CarePackageMultiplier,
                ModSettingsData.MinCarePackageMultiplier,
                ModSettingsData.MaxCarePackageMultiplier);
            settings.DefaultNaturalTileMass = Mathf.Clamp(
                settings.DefaultNaturalTileMass, 1, 2000);
            settings.DefaultNaturalBackwallMass = Mathf.Clamp(
                settings.DefaultNaturalBackwallMass, 1, 2000);
            settings.NaturalConstructionMassMultiplier = Mathf.Clamp(
                settings.NaturalConstructionMassMultiplier, 1f, 2f);
            if (settings.CarePackages == null || settings.CarePackages.Length == 0)
                settings.CarePackages = CarePackageDefaults.Create();
            UpgradeCarePackageSettings(settings);
            settings.CarePackageMultiplier = Mathf.Clamp(
                settings.CarePackageMultiplier,
                ModSettingsData.MinCarePackageMultiplier,
                ModSettingsData.MaxCarePackageMultiplier);
            return settings;
        }

        private static void UpgradeCarePackageSettings(ModSettingsData settings)
        {
            if (settings.CarePackageDefaultsVersion >=
                CurrentCarePackageSettingsVersion)
                return;

            if (settings.CarePackageDefaultsVersion < 1)
            {
                var packages = new List<CarePackageDefinition>(settings.CarePackages);
                bool hasPrimoGarb = packages.Exists(package =>
                    package != null && string.Equals(
                        package.Id, CarePackageDefaults.PrimoGarbId,
                        StringComparison.OrdinalIgnoreCase));
                if (!hasPrimoGarb)
                    packages.Add(new CarePackageDefinition(
                        CarePackageDefaults.PrimoGarbId, 1));
                settings.CarePackages = packages.ToArray();
            }

            if (settings.CarePackageDefaultsVersion < 3)
            {
                if (settings.LegacyDoublePrintingPodItems == true)
                    settings.CarePackageMultiplier = Mathf.Min(
                        ModSettingsData.MaxCarePackageMultiplier,
                        settings.CarePackageMultiplier * 2);
                settings.LegacyDoublePrintingPodItems = null;
            }

            if (settings.CarePackageDefaultsVersion < 4)
            {
                var packages = new List<CarePackageDefinition>(settings.CarePackages);
                CarePackageDefaults.AddMissing(
                    packages, CarePackageDefaults.CreateMetalPackages());
                settings.CarePackages = packages.ToArray();
            }

            if (settings.CarePackageDefaultsVersion < 5)
            {
                var packages = new List<CarePackageDefinition>(settings.CarePackages);
                CarePackageDefaults.AddMissing(packages, new[]
                {
                    CarePackageDefaults.CreateFossilPackage()
                });
                settings.CarePackages = packages.ToArray();
            }

            settings.CarePackageDefaultsVersion =
                CurrentCarePackageSettingsVersion;
        }
    }
}
