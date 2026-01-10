using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Models;

namespace Tracker.Services
{
    public interface IDataService
    {
        // Habits
        List<Habit> GetAllHabits();
        Habit GetHabitById(Guid id);
        void SaveHabit(Habit habit);
        void DeleteHabit(Guid id);
        void UpdateHabitOrder(List<Habit> habits);
        void ToggleHabitCompletion(Guid habitId, DateTime date, string? note = null);
        bool IsHabitCompletedOnDate(Guid habitId, DateTime date);
        
        // Tasks
        List<TodoTask> GetAllTasks();
        TodoTask GetTaskById(Guid id);
        void SaveTask(TodoTask task);
        void DeleteTask(Guid id);
        void UpdateTaskOrder(List<TodoTask> tasks);
        void ToggleTaskCompletion(Guid taskId);
        void ToggleSubTaskCompletion(Guid taskId, Guid subTaskId);
        
        // Statistics
        HabitStatistics GetHabitStatistics(Guid habitId);
        List<HabitStatistics> GetAllHabitStatistics();
        TaskStatistics GetTaskStatistics();
    }

    public class DataService : IDataService
    {
        private List<Habit> _habits;
        private List<TodoTask> _tasks;

        public DataService()
        {
            _habits = new List<Habit>();
            _tasks = new List<TodoTask>();
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

            // Set parent task IDs for subtasks
            foreach (var subTask in task1.SubTasks)
            {
                subTask.ParentTaskId = task1.Id;
            }
        }

        // Habit methods
        public List<Habit> GetAllHabits() => _habits.OrderBy(h => h.DisplayOrder).ToList();

        public Habit GetHabitById(Guid id) => _habits.FirstOrDefault(h => h.Id == id);

        public void SaveHabit(Habit habit)
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
        }

        public void DeleteHabit(Guid id)
        {
            var habit = _habits.FirstOrDefault(h => h.Id == id);
            if (habit != null)
            {
                _habits.Remove(habit);
            }
        }

        public void UpdateHabitOrder(List<Habit> habits)
        {
            for (int i = 0; i < habits.Count; i++)
            {
                habits[i].DisplayOrder = i;
            }
        }

        public void ToggleHabitCompletion(Guid habitId, DateTime date, string? note = null)
        {
            var habit = GetHabitById(habitId);
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

        public bool IsHabitCompletedOnDate(Guid habitId, DateTime date)
        {
            var habit = GetHabitById(habitId);
            if (habit == null) return false;

            return habit.Completions.Any(c => c.CompletedDate.Date == date.Date);
        }

        // Task methods
        public List<TodoTask> GetAllTasks() => _tasks.OrderBy(t => t.DisplayOrder).ToList();

        public TodoTask GetTaskById(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

        public void SaveTask(TodoTask task)
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
            _tasks.Add(task);
        }

        public void DeleteTask(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public void UpdateTaskOrder(List<TodoTask> tasks)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].DisplayOrder = i;
            }
        }

        public void ToggleTaskCompletion(Guid taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;

            task.IsCompleted = !task.IsCompleted;
            task.CompletedDate = task.IsCompleted ? DateTime.Now : null;
        }

        public void ToggleSubTaskCompletion(Guid taskId, Guid subTaskId)
        {
            var task = GetTaskById(taskId);
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
        public HabitStatistics GetHabitStatistics(Guid habitId)
        {
            var habit = GetHabitById(habitId);
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

        public List<HabitStatistics> GetAllHabitStatistics()
        {
            return _habits.OrderBy(h => h.DisplayOrder)
                .Select(h => GetHabitStatistics(h.Id))
                .Where(s => s != null)
                .ToList();
        }

        public TaskStatistics GetTaskStatistics()
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
                LateRate = Math.Round(lateRate, 1)
            };
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
