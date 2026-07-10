using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ONIUtilityTweaks.Support;
using UnityEngine;

namespace ONIUtilityTweaks.ScheduleSync
{
    internal static class ScheduleTemplateStore
    {
        private const string FileName = "schedule_templates.json";

        public static ScheduleTemplateFile LoadFile()
        {
            var candidates = GetCandidatePaths()
                .Where(File.Exists)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            foreach (var file in candidates)
            {
                try
                {
                    var template = JsonConvert.DeserializeObject<ScheduleTemplateFile>(File.ReadAllText(file.FullName));
                    Normalize(template);
                    if (template?.Templates != null && template.Templates.Count > 0)
                        return template;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ONIUtilityTweaks] Could not read schedule template '{file.FullName}': {ex.Message}");
                }
            }

            return null;
        }

        public static List<ScheduleTemplateSet> LoadTemplates()
        {
            return LoadFile()?.Templates ?? new List<ScheduleTemplateSet>();
        }

        public static void Upsert(ScheduleTemplateSet template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Name) || template.Schedules.Count == 0)
                return;

            var file = LoadFile() ?? new ScheduleTemplateFile();
            file.Templates.RemoveAll(existing => string.Equals(existing.Name, template.Name, StringComparison.OrdinalIgnoreCase));
            file.Templates.Add(template);
            file.Templates = file.Templates.OrderBy(existing => existing.Name).ToList();
            SaveFile(file);
        }

        public static void Remove(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return;

            var file = LoadFile();
            if (file == null)
                return;

            file.Templates.RemoveAll(existing => string.Equals(existing.Name, templateName, StringComparison.OrdinalIgnoreCase));
            SaveFile(file);
        }

        private static void SaveFile(ScheduleTemplateFile template)
        {
            Normalize(template);
            if (template == null)
                return;

            var json = JsonConvert.SerializeObject(template, Formatting.Indented);
            foreach (var path in GetWritePaths())
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ONIUtilityTweaks] Could not write schedule template '{path}': {ex.Message}");
                }
            }
        }

        private static void Normalize(ScheduleTemplateFile file)
        {
            if (file == null)
                return;

            file.Templates ??= new List<ScheduleTemplateSet>();
            file.Schedules ??= new List<SavedSchedule>();

            if (file.Templates.Count == 0 && file.Schedules.Count > 0)
            {
                file.Templates.Add(new ScheduleTemplateSet
                {
                    Name = "Auto Saved",
                    SavedAtUtc = System.DateTime.UtcNow.ToString("o"),
                    Schedules = file.Schedules
                });
            }

            file.Schedules.Clear();
        }

        private static IEnumerable<string> GetWritePaths()
        {
            yield return Path.Combine(ModPaths.ConfigFolder, FileName);

            if (!Directory.Exists(ModPaths.CloudSaveRoot))
                yield break;

            foreach (var accountDirectory in Directory.GetDirectories(ModPaths.CloudSaveRoot))
                yield return Path.Combine(accountDirectory, ModPaths.ModFolderName, FileName);
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            yield return Path.Combine(ModPaths.ConfigFolder, FileName);

            var legacyLocalPath = Path.Combine(ModPaths.OniDocuments, "mods", "config", ModPaths.LegacyModFolderName, FileName);
            if (File.Exists(legacyLocalPath))
                yield return legacyLocalPath;

            if (!Directory.Exists(ModPaths.CloudSaveRoot))
                yield break;

            foreach (var accountDirectory in Directory.GetDirectories(ModPaths.CloudSaveRoot))
            {
                yield return Path.Combine(accountDirectory, ModPaths.ModFolderName, FileName);

                var legacyCloudPath = Path.Combine(accountDirectory, ModPaths.LegacyModFolderName, FileName);
                if (File.Exists(legacyCloudPath))
                    yield return legacyCloudPath;
            }
        }
    }
}
