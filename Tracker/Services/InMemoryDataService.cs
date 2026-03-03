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
        Task UpdateHabitDisplayOrderAsync(Guid habitId, int displayOrder);
        Task ToggleHabitCompletionAsync(Guid habitId, DateTime date, string? note = null);
        Task<bool> IsHabitCompletedOnDateAsync(Guid habitId, DateTime date);
        Task<string?> GetHabitNoteAsync(Guid habitId, DateTime date);
        Task SaveHabitNoteAsync(Guid habitId, DateTime date, string noteText);

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

    public class InMemoryDataService : IDataService
    {
        private readonly List<Habit> _habits;
        private readonly List<TodoTask> _tasks;

        public InMemoryDataService()
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

            // Add initial tracking periods for sample habits
            exerciseHabit.TrackingPeriods.Add(new HabitTrackingPeriod
            {
                HabitId = exerciseHabit.Id,
                StartDate = exerciseHabit.CreatedDate.Date,
                EndDate = null,
                TrackEveryday = true,
                TrackingDays = new()
            });

            readingHabit.TrackingPeriods.Add(new HabitTrackingPeriod
            {
                HabitId = readingHabit.Id,
                StartDate = readingHabit.CreatedDate.Date,
                EndDate = null,
                TrackEveryday = false,
                TrackingDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }
            });

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
                // Check if tracking config changed
                var configChanged = existing.TrackEveryday != habit.TrackEveryday;
                if (!configChanged && !habit.TrackEveryday)
                {
                    configChanged = !existing.TrackingDays.OrderBy(d => d)
                        .SequenceEqual(habit.TrackingDays.OrderBy(d => d));
                }

                if (configChanged && habit.TrackingPeriods.Count > 0)
                {
                    var activePeriod = habit.TrackingPeriods.FirstOrDefault(p => p.EndDate == null);
                    if (activePeriod != null)
                    {
                        var today = DateTime.Today;
                        if (activePeriod.StartDate.Date == today)
                        {
                            // Update in place
                            activePeriod.TrackEveryday = habit.TrackEveryday;
                            activePeriod.TrackingDays = new List<DayOfWeek>(habit.TrackingDays);
                        }
                        else
                        {
                            // Close old period, create new one
                            activePeriod.EndDate = today.AddDays(-1);
                            habit.TrackingPeriods.Add(new HabitTrackingPeriod
                            {
                                HabitId = habit.Id,
                                StartDate = today,
                                EndDate = null,
                                TrackEveryday = habit.TrackEveryday,
                                TrackingDays = new List<DayOfWeek>(habit.TrackingDays)
                            });
                        }
                    }
                }

                _habits.Remove(existing);
            }
            else
            {
                habit.DisplayOrder = _habits.Count;
                // Create initial period for new habit
                if (habit.TrackingPeriods.Count == 0)
                {
                    habit.TrackingPeriods.Add(new HabitTrackingPeriod
                    {
                        HabitId = habit.Id,
                        StartDate = habit.CreatedDate.Date,
                        EndDate = null,
                        TrackEveryday = habit.TrackEveryday,
                        TrackingDays = new List<DayOfWeek>(habit.TrackingDays)
                    });
                }
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

        public async Task UpdateHabitDisplayOrderAsync(Guid habitId, int displayOrder)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit != null)
            {
                habit.DisplayOrder = displayOrder;
            }
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

        public async Task<string?> GetHabitNoteAsync(Guid habitId, DateTime date)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit == null) return null;

            var note = habit.Notes.FirstOrDefault(n => n.Date.Date == date.Date);
            return note?.Text;
        }

        public async Task SaveHabitNoteAsync(Guid habitId, DateTime date, string noteText)
        {
            var habit = await GetHabitByIdAsync(habitId);
            if (habit == null) return;

            var dateOnly = date.Date;
            var existingNote = habit.Notes.FirstOrDefault(n => n.Date.Date == dateOnly);

            if (string.IsNullOrWhiteSpace(noteText))
            {
                // If note text is empty, remove the note if it exists
                if (existingNote != null)
                {
                    habit.Notes.Remove(existingNote);
                }
            }
            else
            {
                if (existingNote != null)
                {
                    // Update existing note
                    existingNote.Text = noteText;
                }
                else
                {
                    // Add new note
                    habit.Notes.Add(new HabitNote
                    {
                        HabitId = habitId,
                        Date = dateOnly,
                        Text = noteText
                    });
                }
            }
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

                // Auto-complete parent task if toggle is enabled and all subtasks are done
                if (task.AutoCompleteWithSubtasks && task.SubTaskCompletionPercentage == 100 && !task.IsCompleted)
                {
                    task.IsCompleted = true;
                    task.CompletedDate = DateTime.Now;
                }
            }
        }

        // Statistics methods
        public Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId)
        {
            var habit = _habits.FirstOrDefault(h => h.Id == habitId);
            if (habit == null) return Task.FromResult<HabitStatistics?>(null);

            return Task.FromResult<HabitStatistics?>(StatisticsCalculator.CalculateHabitStatistics(habit));
        }

        public Task<List<HabitStatistics>> GetAllHabitStatisticsAsync()
        {
            return Task.FromResult(StatisticsCalculator.CalculateAllHabitStatistics(_habits));
        }

        public Task<TaskStatistics> GetTaskStatisticsAsync()
        {
            return Task.FromResult(StatisticsCalculator.CalculateTaskStatistics(_tasks));
        }
    }
}
