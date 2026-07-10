using System.Collections.Generic;

namespace ONIUtilityTweaks.ScheduleSync
{
    internal sealed class ScheduleTemplateFile
    {
        public int Version { get; set; } = 1;
        public List<ScheduleTemplateSet> Templates { get; set; } = new List<ScheduleTemplateSet>();

        // Legacy single-template shape used by the first internal build.
        public List<SavedSchedule> Schedules { get; set; } = new List<SavedSchedule>();
    }

    internal sealed class ScheduleTemplateSet
    {
        public string Name { get; set; }
        public string SavedAtUtc { get; set; }
        public List<SavedSchedule> Schedules { get; set; } = new List<SavedSchedule>();
    }

    internal sealed class SavedSchedule
    {
        public string Name { get; set; }
        public bool AlarmActivated { get; set; }
        public List<string> BlockGroupIds { get; set; } = new List<string>();
    }
}
