using Tracker.Models;

namespace Tracker.Services
{
    /// <summary>
    /// Shared statistics calculation logic used by all IDataService implementations.
    /// Operates on already-loaded domain models — no database dependency.
    /// </summary>
    public static class StatisticsCalculator
    {
        public static HabitStatistics CalculateHabitStatistics(Habit habit)
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            // Build HashSet for O(1) completion lookups instead of O(n) per check
            var completionDates = habit.Completions.Select(c => c.CompletedDate.Date).ToHashSet();

            var dailyCompletions = completionDates.Contains(today) ? 1 : 0;
            var weeklyCompletions = habit.Completions.Count(c => c.CompletedDate >= weekStart);
            var monthlyCompletions = habit.Completions.Count(c => c.CompletedDate >= monthStart);
            var yearlyCompletions = habit.Completions.Count(c => c.CompletedDate >= yearStart);

            // Calculate targets using period-aware day-by-day iteration
            var weeklyTarget = 0;
            for (var d = weekStart; d < weekStart.AddDays(7); d = d.AddDays(1))
            {
                if (habit.ShouldTrackOnDay(d)) weeklyTarget++;
            }

            var monthlyTarget = 0;
            var lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            for (var d = monthStart; d <= lastDayOfMonth; d = d.AddDays(1))
            {
                if (habit.ShouldTrackOnDay(d)) monthlyTarget++;
            }

            var yearlyTarget = 0;
            var lastDayOfYear = new DateTime(today.Year, 12, 31);
            for (var d = yearStart; d <= lastDayOfYear; d = d.AddDays(1))
            {
                if (habit.ShouldTrackOnDay(d)) yearlyTarget++;
            }

            var currentStreak = CalculateCurrentStreak(habit, completionDates);
            var longestStreak = CalculateLongestStreak(habit, completionDates);

            // Calculate all-time expected completions
            var allTimeExpected = 0;
            var date = habit.CreatedDate.Date;
            while (date <= today)
            {
                if (habit.ShouldTrackOnDay(date))
                {
                    allTimeExpected++;
                }
                date = date.AddDays(1);
            }

            var allTimeCompletions = habit.Completions.Count;
            var totalDays = (today - habit.CreatedDate).Days + 1;
            var completionRate = totalDays > 0 ? (double)habit.Completions.Count / totalDays * 100 : 0;

            // Calculate yearly breakdown for all years prior to current year
            var yearlyBreakdown = new List<YearlyHabitStatistics>();
            var createdYear = habit.CreatedDate.Year;
            var currentYear = today.Year;

            for (int year = createdYear; year < currentYear; year++)
            {
                var yearStartDate = new DateTime(year, 1, 1);
                var yearEndDate = new DateTime(year, 12, 31);

                // Adjust start date if habit was created mid-year
                if (year == createdYear)
                {
                    yearStartDate = habit.CreatedDate.Date;
                }

                // Count completions for this year
                var completedDays = habit.Completions.Count(c => c.CompletedDate.Year == year);

                // Calculate expected days for this year
                var expectedDays = 0;
                var currentDate = yearStartDate;
                while (currentDate <= yearEndDate)
                {
                    if (habit.ShouldTrackOnDay(currentDate))
                    {
                        expectedDays++;
                    }
                    currentDate = currentDate.AddDays(1);
                }

                var yearCompletionRate = expectedDays > 0 ? Math.Round((double)completedDays / expectedDays * 100, 1) : 0;

                yearlyBreakdown.Add(new YearlyHabitStatistics
                {
                    Year = year,
                    CompletedDays = completedDays,
                    ExpectedDays = expectedDays,
                    CompletionRate = yearCompletionRate
                });
            }

            // Order by year descending (most recent first)
            yearlyBreakdown = yearlyBreakdown.OrderByDescending(y => y.Year).ToList();

            return new HabitStatistics
            {
                HabitId = habit.Id,
                HabitName = habit.Name,
                DailyCompletions = dailyCompletions,
                DailyTarget = 1,
                WeeklyCompletions = weeklyCompletions,
                WeeklyTarget = weeklyTarget,
                MonthlyCompletions = monthlyCompletions,
                MonthlyTarget = monthlyTarget,
                YearlyCompletions = yearlyCompletions,
                YearlyTarget = yearlyTarget,
                AllTimeCompletions = allTimeCompletions,
                AllTimeExpected = allTimeExpected,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                CompletionRate = Math.Round(completionRate, 1),
                YearlyBreakdown = yearlyBreakdown
            };
        }

        public static List<HabitStatistics> CalculateAllHabitStatistics(List<Habit> habits)
        {
            var statistics = new List<HabitStatistics>();
            foreach (var habit in habits.OrderBy(h => h.DisplayOrder))
            {
                statistics.Add(CalculateHabitStatistics(habit));
            }
            return statistics;
        }

        public static TaskStatistics CalculateTaskStatistics(List<TodoTask> tasks)
        {
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.IsCompleted);
            var pendingTasks = totalTasks - completedTasks;
            var tasksWithDeadlines = tasks.Count(t => t.DueDate.HasValue);
            var completedOnTime = tasks.Count(t => t.CompletedOnTime);
            var completedAfterDeadline = tasks.Count(t => t.CompletedAfterDeadline);

            var completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
            var onTimeRate = tasksWithDeadlines > 0 ? (double)completedOnTime / tasksWithDeadlines * 100 : 0;
            var lateRate = tasksWithDeadlines > 0 ? (double)completedAfterDeadline / tasksWithDeadlines * 100 : 0;

            // Calculate yearly breakdown
            var yearlyBreakdown = tasks
                .GroupBy(t => t.CreatedDate.Year)
                .OrderByDescending(g => g.Key)
                .Select(g => new YearlyTaskStatistics
                {
                    Year = g.Key,
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(t => t.IsCompleted),
                    CompletedOverdue = g.Count(t => t.CompletedAfterDeadline),
                    CompletionRate = g.Count() > 0 ? Math.Round((double)g.Count(t => t.IsCompleted) / g.Count() * 100, 1) : 0,
                    CompletedOverdueRate = g.Count() > 0 ? Math.Round((double)g.Count(t => t.CompletedAfterDeadline) / g.Count() * 100, 1) : 0
                })
                .ToList();

            return new TaskStatistics
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                CompletedOnTime = completedOnTime,
                CompletedAfterDeadline = completedAfterDeadline,
                TasksWithDeadlines = tasksWithDeadlines,
                CompletionRate = Math.Round(completionRate, 1),
                OnTimeRate = Math.Round(onTimeRate, 1),
                LateRate = Math.Round(lateRate, 1),
                YearlyBreakdown = yearlyBreakdown
            };
        }

        private static int CalculateCurrentStreak(Habit habit, HashSet<DateTime> completionDates)
        {
            var streak = 0;
            var date = DateTime.Today;
            var earliestDate = habit.CreatedDate.Date;

            while (date >= earliestDate)
            {
                if (habit.ShouldTrackOnDay(date))
                {
                    if (completionDates.Contains(date))
                    {
                        streak++;
                    }
                    else
                    {
                        break;
                    }
                }
                date = date.AddDays(-1);
            }

            return streak;
        }

        private static int CalculateLongestStreak(Habit habit, HashSet<DateTime> completionDates)
        {
            if (completionDates.Count == 0) return 0;

            var longestStreak = 0;
            var currentStreak = 0;
            var date = habit.CreatedDate.Date;
            var today = DateTime.Today;

            while (date <= today)
            {
                if (habit.ShouldTrackOnDay(date))
                {
                    if (completionDates.Contains(date))
                    {
                        currentStreak++;
                        longestStreak = Math.Max(longestStreak, currentStreak);
                    }
                    else
                    {
                        currentStreak = 0;
                    }
                }
                date = date.AddDays(1);
            }

            return longestStreak;
        }
    }
}
