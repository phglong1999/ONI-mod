using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ONIUtilityTweaks.ScheduleSync
{
    internal static class ScheduleTemplateSync
    {
        private const int BlocksPerTimetable = 24;

        public static ScheduleTemplateSet CaptureCurrent(string templateName, IEnumerable<Schedule> selectedSchedules)
        {
            if (ScheduleManager.Instance == null)
                throw new InvalidOperationException("ScheduleManager is not ready.");

            var schedules = selectedSchedules?.ToList() ?? ScheduleManager.Instance.GetSchedules();
            return Capture(templateName, schedules);
        }

        public static void SaveTemplate(string templateName, IEnumerable<Schedule> selectedSchedules)
        {
            var template = CaptureCurrent(templateName, selectedSchedules);
            ScheduleTemplateStore.Upsert(template);
            Debug.Log($"[ONIUtilityTweaks] Saved schedule template '{template.Name}' with {template.Schedules.Count} schedule(s).");
        }

        public static void ApplyTemplate(ScheduleTemplateSet template)
        {
            if (template == null || ScheduleManager.Instance == null)
                return;

            Apply(ScheduleManager.Instance, template);
        }

        private static ScheduleTemplateSet Capture(string templateName, IEnumerable<Schedule> schedules)
        {
            var template = new ScheduleTemplateSet
            {
                Name = string.IsNullOrWhiteSpace(templateName) ? $"Schedule {System.DateTime.Now:yyyy-MM-dd HH-mm}" : templateName.Trim(),
                SavedAtUtc = System.DateTime.UtcNow.ToString("o")
            };

            foreach (var schedule in schedules)
            {
                var blocks = schedule.GetBlocks();
                if (blocks == null || blocks.Count == 0)
                    continue;

                template.Schedules.Add(new SavedSchedule
                {
                    Name = schedule.name,
                    AlarmActivated = schedule.alarmActivated,
                    BlockGroupIds = blocks.Select(block => block.GroupId).ToList()
                });
            }

            return template;
        }

        private static void Apply(ScheduleManager manager, ScheduleTemplateSet template)
        {
            try
            {
                var schedules = manager.GetSchedules();
                var groupById = Db.Get().ScheduleGroups.allGroups.ToDictionary(group => group.Id);
                var changedSchedules = new List<Schedule>();

                for (var i = 0; i < template.Schedules.Count; i++)
                {
                    var saved = template.Schedules[i];
                    if (saved.BlockGroupIds == null || saved.BlockGroupIds.Count == 0)
                        continue;

                    var schedule = i < schedules.Count ? schedules[i] : AddScheduleFromTemplate(schedules, saved);
                    ApplySchedule(schedule, saved, groupById);
                    changedSchedules.Add(schedule);
                }

                NotifyScheduleUi(manager, schedules, changedSchedules);
                Debug.Log($"[ONIUtilityTweaks] Applied {template.Schedules.Count} saved schedule template(s).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ONIUtilityTweaks] Could not apply schedule template: {ex}");
            }
        }

        private static Schedule AddScheduleFromTemplate(ICollection<Schedule> schedules, SavedSchedule saved)
        {
            var blocks = saved.BlockGroupIds
                .Select(groupId => new ScheduleBlock(groupId, groupId))
                .ToList();

            var schedule = new Schedule(saved.Name, blocks, saved.AlarmActivated);
            schedules.Add(schedule);
            return schedule;
        }

        private static void ApplySchedule(Schedule schedule, SavedSchedule saved, IDictionary<string, ScheduleGroup> groupById)
        {
            schedule.name = saved.Name;
            schedule.alarmActivated = saved.AlarmActivated;
            EnsureBlockCount(schedule, saved.BlockGroupIds);

            var blocksToApply = Math.Min(schedule.GetBlocks().Count, saved.BlockGroupIds.Count);
            for (var blockIndex = 0; blockIndex < blocksToApply; blockIndex++)
            {
                var groupId = saved.BlockGroupIds[blockIndex];
                if (groupById.TryGetValue(groupId, out var group))
                    schedule.SetBlockGroup(blockIndex, group);
            }
        }

        private static void NotifyScheduleUi(ScheduleManager manager, List<Schedule> schedules, List<Schedule> changedSchedules)
        {
            try
            {
                var changedMethod = AccessTools.Method(typeof(Schedule), "Changed");
                foreach (var schedule in changedSchedules)
                    changedMethod?.Invoke(schedule, null);

                var schedulesChanged = AccessTools.Field(typeof(ScheduleManager), "onSchedulesChanged")?.GetValue(manager) as Action<List<Schedule>>;
                schedulesChanged?.Invoke(schedules);

                var screen = ScheduleScreen.Instance;
                if (screen == null)
                    return;

                screen.OnChangeCurrentTimetable();
                AccessTools.Method(typeof(ScheduleScreen), "RefreshWidgetWorldData")?.Invoke(screen, new object[] { null });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ONIUtilityTweaks] Could not refresh schedule screen after loading template: {ex}");
            }
        }

        private static void EnsureBlockCount(Schedule schedule, List<string> targetGroupIds)
        {
            var targetRows = Math.Max(1, targetGroupIds.Count / BlocksPerTimetable);
            var currentRows = Math.Max(1, schedule.GetBlocks().Count / BlocksPerTimetable);

            while (currentRows < targetRows)
            {
                var start = currentRows * BlocksPerTimetable;
                var row = targetGroupIds
                    .Skip(start)
                    .Take(BlocksPerTimetable)
                    .Select(groupId => new ScheduleBlock(groupId, groupId))
                    .ToList();

                schedule.AddTimetable(row);
                currentRows++;
            }

            while (currentRows > targetRows)
            {
                schedule.RemoveTimetable(currentRows - 1);
                currentRows--;
            }
        }
    }

}
