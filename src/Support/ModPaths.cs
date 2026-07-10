using System;
using System.IO;

namespace ONIUtilityTweaks.Support
{
    internal static class ModPaths
    {
        public const string ModFolderName = "ONIUtilityTweaks";
        public const string LegacyModFolderName = "DoubleWireCapacity";

        public static string OniDocuments => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Klei",
            "OxygenNotIncluded");

        public static string ConfigFolder => Path.Combine(
            OniDocuments,
            "mods",
            "config",
            ModFolderName);

        public static string CloudSaveRoot => Path.Combine(OniDocuments, "cloud_save_files");
    }
}
