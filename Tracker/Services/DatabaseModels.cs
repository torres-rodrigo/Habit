using SQLite;
using System;

namespace Tracker.Services
{
    /// <summary>
    /// Database table for Habits with SQLite attributes
    /// </summary>
    [Table("Habits")]
    public class HabitDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool TrackEveryday { get; set; }
        public string CreatedDateUtc { get; set; } = string.Empty; // ISO 8601 format
        public string? DeadlineUtc { get; set; } // ISO 8601 format, nullable
        public bool HasReminders { get; set; }
        public long? ReminderTimeTicks { get; set; } // TimeSpan stored as ticks
        public bool NotesEnabled { get; set; }
        public bool IsNegativeHabit { get; set; }
        public bool IsTracked { get; set; } = true;
        public string? UntrackedDateUtc { get; set; } // ISO 8601 format, nullable
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Junction table for Habit tracking days (many-to-many relationship)
    /// </summary>
    [Table("HabitTrackingDays")]
    public class HabitTrackingDayDb
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string HabitId { get; set; } = string.Empty;

        public int DayOfWeek { get; set; } // 0-6 (Sunday-Saturday)
    }

    /// <summary>
    /// Database table for Habit completions
    /// </summary>
    [Table("HabitCompletions")]
    public class HabitCompletionDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string HabitId { get; set; } = string.Empty;

        [Indexed]
        public string CompletedDateUtc { get; set; } = string.Empty; // Date only in "yyyy-MM-dd" format

        public string? Note { get; set; }
    }

    /// <summary>
    /// Database table for Tasks
    /// </summary>
    [Table("Tasks")]
    public class TaskDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedDateUtc { get; set; } = string.Empty; // ISO 8601 format
        public string? DueDateUtc { get; set; } // ISO 8601 format, nullable
        public string? Priority { get; set; }
        public string? CompletedDateUtc { get; set; } // ISO 8601 format, nullable
        public bool IsCompleted { get; set; }
        public bool HasReminders { get; set; }
        public long? ReminderTimeTicks { get; set; } // TimeSpan stored as ticks
        public int DisplayOrder { get; set; }
        public bool AutoCompleteWithSubtasks { get; set; }
        public bool IsPinned { get; set; }
    }

    /// <summary>
    /// Database table for SubTasks
    /// </summary>
    [Table("SubTasks")]
    public class SubTaskDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string ParentTaskId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Database table for version tracking and metadata
    /// </summary>
    [Table("DatabaseInfo")]
    public class DatabaseInfoDb
    {
        [PrimaryKey]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
