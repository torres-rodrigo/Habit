using System;
using System.Collections.Generic;

namespace Tracker.Models
{
    public class Habit
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DayOfWeek> TrackingDays { get; set; } = new();
        public bool TrackEveryday { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? Deadline { get; set; }
        public bool HasReminders { get; set; }
        public TimeSpan? ReminderTime { get; set; }
        public bool NotesEnabled { get; set; }
        public bool IsNegativeHabit { get; set; }
        public bool IsTracked { get; set; } = true;
        public DateTime? UntrackedDate { get; set; }
        public int DisplayOrder { get; set; }
        public List<HabitCompletion> Completions { get; set; } = new();
        public List<HabitNote> Notes { get; set; } = new();
    }

    public class HabitCompletion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HabitId { get; set; }
        public DateTime CompletedDate { get; set; }
        public string? Note { get; set; }
    }

    public class HabitNote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HabitId { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
