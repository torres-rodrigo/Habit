using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Models;

namespace Tracker.Services
{
    public interface IDataService
    {
        // Habits
        Task<List<Habit>> GetAllHabitsAsync();
        Task<Habit?> GetHabitByIdAsync(Guid id);
        Task SaveHabitAsync(Habit habit);
        Task DeleteHabitAsync(Guid id);
        Task UpdateHabitOrderAsync(List<Habit> habits);
        Task ToggleHabitCompletionAsync(Guid habitId, DateTime date, string? note = null);
        Task<bool> IsHabitCompletedOnDateAsync(Guid habitId, DateTime date);

        // Tasks
        Task<List<TodoTask>> GetAllTasksAsync();
        Task<TodoTask?> GetTaskByIdAsync(Guid id);
        Task SaveTaskAsync(TodoTask task);
        Task DeleteTaskAsync(Guid id);
        Task UpdateTaskOrderAsync(List<TodoTask> tasks);
        Task ToggleTaskCompletionAsync(Guid taskId);
        Task ToggleSubTaskCompletionAsync(Guid taskId, Guid subTaskId);

        // Statistics
        Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId);
        Task<List<HabitStatistics>> GetAllHabitStatisticsAsync();
        Task<TaskStatistics> GetTaskStatisticsAsync();
    }

    public class DataService : IDataService
    {
        private readonly List<Habit> _habits;
        private readonly List<TodoTask> _tasks;

        public DataService()
        {
            _habits = new();
            _tasks = new();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // Sample habits
            var exerciseHabit = new Habit
            {
                Name = "Morning Exercise",
                Description = "30 minutes of exercise",
                TrackEveryday = true,
                NotesEnabled = true,
                DisplayOrder = 0,
                HasReminders = true,
                ReminderTime = new TimeSpan(7, 0, 0)
            };

            var readingHabit = new Habit
            {
                Name = "Reading",
                Description = "Read for 20 minutes",
                TrackEveryday = false,
                TrackingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                NotesEnabled = false,
                DisplayOrder = 1,
                HasReminders = true,
                ReminderTime = new TimeSpan(20, 0, 0)
            };

            _habits.Add(exerciseHabit);
            _habits.Add(readingHabit);

            // Add some sample completions
            var today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-i);
                if (i % 2 == 0)
                {
                    _habits[0].Completions.Add(new HabitCompletion
                    {
                        HabitId = exerciseHabit.Id,
                        CompletedDate = date,
                        Note = i == 0 ? "Felt great today!" : null
                    });
                }
            }

            // Sample tasks
            var task1 = new TodoTask
            {
                Name = "Complete project report",
                Description = "Finish the Q1 report",
                DueDate = DateTime.Today.AddDays(3),
                HasReminders = true,
                ReminderTime = new TimeSpan(9, 0, 0),
                DisplayOrder = 0,
                SubTasks = new List<SubTask>
                {
                    new SubTask { Name = "Gather data", IsCompleted = true, DisplayOrder = 0 },
                    new SubTask { Name = "Write summary", IsCompleted = false, DisplayOrder = 1 },
                    new SubTask { Name = "Review and submit", IsCompleted = false, DisplayOrder = 2 }
                }
            };

            var task2 = new TodoTask
            {
                Name = "Buy groceries",
                Description = "Weekly shopping",
                IsCompleted = true,
                CompletedDate = DateTime.Today.AddDays(-1),
                DisplayOrder = 1
            };

            _tasks.Add(task1);
            _tasks.Add(task2);

            // Set parent task references for subtasks
            SetSubTaskParentReferences(task1);
        }

        private void SetSubTaskParentReferences(TodoTask task)
        {
            foreach (var subTask in task.SubTasks)
            {
                subTask.ParentTaskId = task.Id;
                subTask.SetParentTask(task);
            }
        }

        // Habit methods
        public Task<List<Habit>> GetAllHabitsAsync()
        {
            var habits = _habits.OrderBy(h => h.DisplayOrder).ToList();
            return Task.FromResult(habits);
        }

        public Task<Habit?> GetHabitByIdAsync(Guid id)
        {
            var habit = _habits.FirstOrDefault(h => h.Id == id);
            return Task.FromResult(habit);
        }

        public Task SaveHabitAsync(Habit habit)
        {
            var existing = _habits.FirstOrDefault(h => h.Id == habit.Id);
            if (existing != null)
            {
                _habits.Remove(existing);
            }
            else
            {
                habit.DisplayOrder = _habits.Count;
            }
            _habits.Add(habit);
            return Task.CompletedTask;
        }

        public Task DeleteHabitAsync(Guid id)
        {
            var habit = _habits.FirstOrDefault(h => h.Id == id);
            if (habit != null)
            {
                _habits.Remove(habit);
            }
            return Task.CompletedTask;
        }

        public Task UpdateHabitOrderAsync(List<Habit> habits)
        {
            for (int i = 0; i < habits.Count; i++)
            {
                habits[i].DisplayOrder = i;
            }
            return Task.CompletedTask;
        }

        public async Task ToggleHabitCompletionAsync(Guid habitId, DateTime date, string? note = null)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit == null) return;

            var dateOnly = date.Date;
            var existing = habit.Completions.FirstOrDefault(c => c.CompletedDate.Date == dateOnly);

            if (existing != null)
            {
                habit.Completions.Remove(existing);
            }
            else
            {
                habit.Completions.Add(new HabitCompletion
                {
                    HabitId = habitId,
                    CompletedDate = dateOnly,
                    Note = note
                });
            }
        }

        public async Task<bool> IsHabitCompletedOnDateAsync(Guid habitId, DateTime date)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit == null) return false;

            return habit.Completions.Any(c => c.CompletedDate.Date == date.Date);
        }

        // Task methods
        public Task<List<TodoTask>> GetAllTasksAsync()
        {
            var tasks = _tasks.OrderBy(t => t.DisplayOrder).ToList();
            foreach (var task in tasks)
            {
                SetSubTaskParentReferences(task);
            }
            return Task.FromResult(tasks);
        }

        public Task<TodoTask?> GetTaskByIdAsync(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                SetSubTaskParentReferences(task);
            }
            return Task.FromResult(task);
        }

        public Task SaveTaskAsync(TodoTask task)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                _tasks.Remove(existing);
            }
            else
            {
                task.DisplayOrder = _tasks.Count;
            }

            // Set parent references for all subtasks
            SetSubTaskParentReferences(task);

            _tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
            }
            return Task.CompletedTask;
        }

        public Task UpdateTaskOrderAsync(List<TodoTask> tasks)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].DisplayOrder = i;
            }
            return Task.CompletedTask;
        }

        public async Task ToggleTaskCompletionAsync(Guid taskId)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return;

            task.IsCompleted = !task.IsCompleted;
            task.CompletedDate = task.IsCompleted ? DateTime.Now : null;
        }

        public async Task ToggleSubTaskCompletionAsync(Guid taskId, Guid subTaskId)
        {
            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return;

            var subTask = task.SubTasks.FirstOrDefault(st => st.Id == subTaskId);
            if (subTask != null)
            {
                subTask.IsCompleted = !subTask.IsCompleted;

                // Auto-complete parent task if all subtasks are completed
                if (task.AllSubTasksCompleted && !task.IsCompleted)
                {
                    task.IsCompleted = true;
                    task.CompletedDate = DateTime.Now;
                }
            }
        }

        // Statistics methods
        public async Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit == null) return null;

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var dailyCompletions = habit.Completions.Count(c => c.CompletedDate.Date == today);
            var weeklyCompletions = habit.Completions.Count(c => c.CompletedDate >= weekStart);
            var monthlyCompletions = habit.Completions.Count(c => c.CompletedDate >= monthStart);
            var yearlyCompletions = habit.Completions.Count(c => c.CompletedDate >= yearStart);

            var weeklyTarget = habit.TrackEveryday ? 7 : habit.TrackingDays.Count;
            var monthlyTarget = habit.TrackEveryday ? DateTime.DaysInMonth(today.Year, today.Month) :
                GetDaysInMonthForWeekdays(today.Year, today.Month, habit.TrackingDays);
            var yearlyTarget = habit.TrackEveryday ? (DateTime.IsLeapYear(today.Year) ? 366 : 365) :
                GetDaysInYearForWeekdays(today.Year, habit.TrackingDays);

            var currentStreak = CalculateCurrentStreak(habit);
            var longestStreak = CalculateLongestStreak(habit);

            var totalDays = (today - habit.CreatedDate).Days + 1;
            var completionRate = totalDays > 0 ? (double)habit.Completions.Count / totalDays * 100 : 0;

            return new HabitStatistics
            {
                HabitId = habitId,
                HabitName = habit.Name,
                DailyCompletions = dailyCompletions,
                DailyTarget = 1,
                WeeklyCompletions = weeklyCompletions,
                WeeklyTarget = weeklyTarget,
                MonthlyCompletions = monthlyCompletions,
                MonthlyTarget = monthlyTarget,
                YearlyCompletions = yearlyCompletions,
                YearlyTarget = yearlyTarget,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                CompletionRate = Math.Round(completionRate, 1)
            };
        }

        public async Task<List<HabitStatistics>> GetAllHabitStatisticsAsync()
        {
            var statistics = new List<HabitStatistics>();
            var habits = _habits.OrderBy(h => h.DisplayOrder);

            foreach (var habit in habits)
            {
                var stat = await GetHabitStatisticsAsync(habit.Id);
                if (stat != null)
                {
                    statistics.Add(stat);
                }
            }

            return statistics;
        }

        public Task<TaskStatistics> GetTaskStatisticsAsync()
        {
            var totalTasks = _tasks.Count;
            var completedTasks = _tasks.Count(t => t.IsCompleted);
            var pendingTasks = totalTasks - completedTasks;
            var tasksWithDeadlines = _tasks.Count(t => t.DueDate.HasValue);
            var completedOnTime = _tasks.Count(t => t.CompletedOnTime);
            var completedAfterDeadline = _tasks.Count(t => t.CompletedAfterDeadline);

            var completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
            var onTimeRate = tasksWithDeadlines > 0 ? (double)completedOnTime / tasksWithDeadlines * 100 : 0;
            var lateRate = tasksWithDeadlines > 0 ? (double)completedAfterDeadline / tasksWithDeadlines * 100 : 0;

            var statistics = new TaskStatistics
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                CompletedOnTime = completedOnTime,
                CompletedAfterDeadline = completedAfterDeadline,
                TasksWithDeadlines = tasksWithDeadlines,
                CompletionRate = Math.Round(completionRate, 1),
                OnTimeRate = Math.Round(onTimeRate, 1),
                LateRate = Math.Round(lateRate, 1)
            };

            return Task.FromResult(statistics);
        }

        private int CalculateCurrentStreak(Habit habit)
        {
            var streak = 0;
            var date = DateTime.Today;

            while (true)
            {
                if (ShouldTrackOnDay(habit, date))
                {
                    if (habit.Completions.Any(c => c.CompletedDate.Date == date))
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

        private int CalculateLongestStreak(Habit habit)
        {
            if (habit.Completions.Count == 0) return 0;

            var longestStreak = 0;
            var currentStreak = 0;
            var date = habit.CreatedDate;
            var today = DateTime.Today;

            while (date <= today)
            {
                if (ShouldTrackOnDay(habit, date))
                {
                    if (habit.Completions.Any(c => c.CompletedDate.Date == date))
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

        private bool ShouldTrackOnDay(Habit habit, DateTime date)
        {
            if (habit.TrackEveryday) return true;
            return habit.TrackingDays.Contains(date.DayOfWeek);
        }

        private int GetDaysInMonthForWeekdays(int year, int month, List<DayOfWeek> weekdays)
        {
            var count = 0;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                if (weekdays.Contains(date.DayOfWeek))
                    count++;
            }
            return count;
        }

        private int GetDaysInYearForWeekdays(int year, List<DayOfWeek> weekdays)
        {
            var count = 0;
            var date = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);

            while (date <= endDate)
            {
                if (weekdays.Contains(date.DayOfWeek))
                    count++;
                date = date.AddDays(1);
            }
            return count;
        }
    }
}
