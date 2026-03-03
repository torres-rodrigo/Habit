using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<HabitTrackingPeriod> TrackingPeriods { get; set; } = new();

        /// <summary>
        /// Period-aware check for whether the habit should be tracked on a given date.
        /// Uses the historically correct tracking period configuration.
        /// Falls back to current TrackEveryday/TrackingDays if no periods are loaded.
        /// </summary>
        public bool ShouldTrackOnDay(DateTime date)
        {
            if (TrackingPeriods.Count > 0)
            {
                var period = TrackingPeriods.FirstOrDefault(p => p.ContainsDate(date));
                return period?.ShouldTrackOnDay(date) ?? false;
            }
            if (TrackEveryday) return true;
            return TrackingDays.Contains(date.DayOfWeek);
        }
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

    public class HabitTrackingPeriod
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HabitId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool TrackEveryday { get; set; }
        public List<DayOfWeek> TrackingDays { get; set; } = new();

        public bool ContainsDate(DateTime date)
        {
            var d = date.Date;
            if (d < StartDate.Date) return false;
            if (EndDate.HasValue && d > EndDate.Value.Date) return false;
            return true;
        }

        public bool ShouldTrackOnDay(DateTime date)
        {
            if (TrackEveryday) return true;
            return TrackingDays.Contains(date.DayOfWeek);
        }
    }
}
