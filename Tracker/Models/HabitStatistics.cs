using System;

namespace Tracker.Models
{
    public class HabitStatistics
    {
        public Guid HabitId { get; set; }
        public string HabitName { get; set; } = string.Empty;
        public int DailyCompletions { get; set; }
        public int DailyTarget { get; set; }
        public int WeeklyCompletions { get; set; }
        public int WeeklyTarget { get; set; }
        public int MonthlyCompletions { get; set; }
        public int MonthlyTarget { get; set; }
        public int YearlyCompletions { get; set; }
        public int YearlyTarget { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public double CompletionRate { get; set; }

        public double WeeklyProgress => WeeklyTarget > 0 ? (double)WeeklyCompletions / WeeklyTarget : 0;
        public double MonthlyProgress => MonthlyTarget > 0 ? (double)MonthlyCompletions / MonthlyTarget : 0;
        public double YearlyProgress => YearlyTarget > 0 ? (double)YearlyCompletions / YearlyTarget : 0;
    }

    public class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedOnTime { get; set; }
        public int CompletedAfterDeadline { get; set; }
        public int TasksWithDeadlines { get; set; }
        public double CompletionRate { get; set; }
        public double OnTimeRate { get; set; }
        public double LateRate { get; set; }
    }
}
