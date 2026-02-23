using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tracker.Models;

namespace Tracker.Services
{
    /// <summary>
    /// SQLite-based implementation of IDataService
    /// Uses async initialization pattern to avoid blocking constructor
    /// </summary>
    public class DatabaseService : IDataService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly Task _initializationTask;
        private const int CurrentDatabaseVersion = 1;

        public DatabaseService()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "tracker.db3");

            _database = new SQLiteAsyncConnection(dbPath);
            _initializationTask = InitializeDatabaseAsync();
        }

        /// <summary>
        /// Ensures database is initialized before any operation
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            await _initializationTask;
        }

        /// <summary>
        /// Initializes database tables, indexes, and version tracking
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            // Create all tables
            await _database.CreateTableAsync<HabitDb>();
            await _database.CreateTableAsync<HabitTrackingDayDb>();
            await _database.CreateTableAsync<HabitCompletionDb>();
            await _database.CreateTableAsync<TaskDb>();
            await _database.CreateTableAsync<SubTaskDb>();
            await _database.CreateTableAsync<DatabaseInfoDb>();

            // Create indexes for performance
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_habit_tracking_days_habit_id ON HabitTrackingDays(HabitId)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_habit_completions_habit_id ON HabitCompletions(HabitId)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_habit_completions_date ON HabitCompletions(CompletedDateUtc)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_subtasks_parent_task_id ON SubTasks(ParentTaskId)");

            // Check and handle database version
            var versionInfo = await _database.Table<DatabaseInfoDb>()
                .Where(d => d.Key == "Version")
                .FirstOrDefaultAsync();

            if (versionInfo == null)
            {
                // New database - set version
                await _database.InsertAsync(new DatabaseInfoDb
                {
                    Key = "Version",
                    Value = CurrentDatabaseVersion.ToString()
                });
            }
            else if (int.TryParse(versionInfo.Value, out var version))
            {
                // Existing database - handle migrations if needed
                if (version < CurrentDatabaseVersion)
                {
                    await MigrateDatabaseAsync(version, CurrentDatabaseVersion);
                }
            }
        }

        /// <summary>
        /// Handles database schema migrations between versions
        /// </summary>
        private async Task MigrateDatabaseAsync(int fromVersion, int toVersion)
        {
            // Placeholder for future migrations
            // Example:
            // if (fromVersion == 1 && toVersion >= 2)
            // {
            //     await _database.ExecuteAsync("ALTER TABLE Habits ADD COLUMN NewField TEXT");
            // }

            // Update version
            await _database.ExecuteAsync(
                "UPDATE DatabaseInfo SET Value = ? WHERE Key = 'Version'",
                toVersion.ToString());
        }

        #region Habit Operations

        public async Task<List<Habit>> GetAllHabitsAsync()
        {
            await EnsureInitializedAsync();

            var habitDbs = await _database.Table<HabitDb>()
                .OrderBy(h => h.DisplayOrder)
                .ToListAsync();

            var habits = new List<Habit>();
            foreach (var habitDb in habitDbs)
            {
                var habit = await MapToHabitAsync(habitDb);
                habits.Add(habit);
            }

            return habits;
        }

        public async Task<Habit?> GetHabitByIdAsync(Guid id)
        {
            await EnsureInitializedAsync();

            var habitDb = await _database.Table<HabitDb>()
                .Where(h => h.Id == id.ToString())
                .FirstOrDefaultAsync();

            if (habitDb == null)
                return null;

            return await MapToHabitAsync(habitDb);
        }

        public async Task SaveHabitAsync(Habit habit)
        {
            await EnsureInitializedAsync();

            var habitId = habit.Id.ToString();
            var existing = await _database.Table<HabitDb>()
                .Where(h => h.Id == habitId)
                .FirstOrDefaultAsync();

            var habitDb = MapToHabitDb(habit);

            // Set DisplayOrder only for new habits
            if (existing == null)
            {
                var maxOrder = await _database.Table<HabitDb>()
                    .OrderByDescending(h => h.DisplayOrder)
                    .FirstOrDefaultAsync();
                habitDb.DisplayOrder = maxOrder?.DisplayOrder + 1 ?? 0;
            }
            else
            {
                habitDb.DisplayOrder = existing.DisplayOrder;
            }

            await _database.RunInTransactionAsync((conn) =>
            {
                // Save or update habit
                if (existing != null)
                {
                    conn.Update(habitDb);
                }
                else
                {
                    conn.Insert(habitDb);
                }

                // Update tracking days (delete all + insert new)
                conn.Execute("DELETE FROM HabitTrackingDays WHERE HabitId = ?", habitId);
                foreach (var day in habit.TrackingDays)
                {
                    conn.Insert(new HabitTrackingDayDb
                    {
                        HabitId = habitId,
                        DayOfWeek = (int)day
                    });
                }

                // Update completions (delete all + insert new)
                conn.Execute("DELETE FROM HabitCompletions WHERE HabitId = ?", habitId);
                foreach (var completion in habit.Completions)
                {
                    conn.Insert(new HabitCompletionDb
                    {
                        Id = completion.Id.ToString(),
                        HabitId = habitId,
                        CompletedDateUtc = completion.CompletedDate.Date.ToString("yyyy-MM-dd"),
                        Note = completion.Note
                    });
                }
            });
        }

        public async Task DeleteHabitAsync(Guid id)
        {
            await EnsureInitializedAsync();

            var habitId = id.ToString();

            await _database.RunInTransactionAsync((conn) =>
            {
                // Cascade delete tracking days
                conn.Execute("DELETE FROM HabitTrackingDays WHERE HabitId = ?", habitId);

                // Cascade delete completions
                conn.Execute("DELETE FROM HabitCompletions WHERE HabitId = ?", habitId);

                // Delete habit
                conn.Execute("DELETE FROM Habits WHERE Id = ?", habitId);
            });
        }

        public async Task UpdateHabitOrderAsync(List<Habit> habits)
        {
            await EnsureInitializedAsync();

            await _database.RunInTransactionAsync((conn) =>
            {
                for (int i = 0; i < habits.Count; i++)
                {
                    habits[i].DisplayOrder = i;
                    conn.Execute(
                        "UPDATE Habits SET DisplayOrder = ? WHERE Id = ?",
                        i,
                        habits[i].Id.ToString());
                }
            });
        }

        public async Task ToggleHabitCompletionAsync(Guid habitId, DateTime date, string? note = null)
        {
            await EnsureInitializedAsync();

            var habitIdStr = habitId.ToString();
            var dateOnly = date.Date.ToString("yyyy-MM-dd");

            var existing = await _database.Table<HabitCompletionDb>()
                .Where(c => c.HabitId == habitIdStr && c.CompletedDateUtc == dateOnly)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Remove completion
                await _database.DeleteAsync(existing);
            }
            else
            {
                // Add completion
                await _database.InsertAsync(new HabitCompletionDb
                {
                    Id = Guid.NewGuid().ToString(),
                    HabitId = habitIdStr,
                    CompletedDateUtc = dateOnly,
                    Note = note
                });
            }
        }

        public async Task<bool> IsHabitCompletedOnDateAsync(Guid habitId, DateTime date)
        {
            await EnsureInitializedAsync();

            var habitIdStr = habitId.ToString();
            var dateOnly = date.Date.ToString("yyyy-MM-dd");

            var completion = await _database.Table<HabitCompletionDb>()
                .Where(c => c.HabitId == habitIdStr && c.CompletedDateUtc == dateOnly)
                .FirstOrDefaultAsync();

            return completion != null;
        }

        #endregion

        #region Habit Mapping Methods

        /// <summary>
        /// Maps domain Habit model to database HabitDb model
        /// </summary>
        private HabitDb MapToHabitDb(Habit habit)
        {
            return new HabitDb
            {
                Id = habit.Id.ToString(),
                Name = habit.Name,
                Description = habit.Description,
                TrackEveryday = habit.TrackEveryday,
                CreatedDateUtc = habit.CreatedDate.ToUniversalTime().ToString("o"),
                DeadlineUtc = habit.Deadline?.ToUniversalTime().ToString("o"),
                HasReminders = habit.HasReminders,
                ReminderTimeTicks = habit.ReminderTime?.Ticks,
                NotesEnabled = habit.NotesEnabled,
                DisplayOrder = habit.DisplayOrder
            };
        }

        /// <summary>
        /// Maps database HabitDb model to domain Habit model
        /// Loads related tracking days and completions
        /// </summary>
        private async Task<Habit> MapToHabitAsync(HabitDb habitDb)
        {
            var habit = new Habit
            {
                Id = Guid.Parse(habitDb.Id),
                Name = habitDb.Name,
                Description = habitDb.Description,
                TrackEveryday = habitDb.TrackEveryday,
                CreatedDate = DateTime.Parse(habitDb.CreatedDateUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                Deadline = string.IsNullOrEmpty(habitDb.DeadlineUtc)
                    ? null
                    : DateTime.Parse(habitDb.DeadlineUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                HasReminders = habitDb.HasReminders,
                ReminderTime = habitDb.ReminderTimeTicks.HasValue
                    ? TimeSpan.FromTicks(habitDb.ReminderTimeTicks.Value)
                    : null,
                NotesEnabled = habitDb.NotesEnabled,
                DisplayOrder = habitDb.DisplayOrder
            };

            // Load tracking days
            var trackingDays = await _database.Table<HabitTrackingDayDb>()
                .Where(t => t.HabitId == habitDb.Id)
                .ToListAsync();
            habit.TrackingDays = trackingDays.Select(d => (DayOfWeek)d.DayOfWeek).ToList();

            // Load completions
            var completions = await _database.Table<HabitCompletionDb>()
                .Where(c => c.HabitId == habitDb.Id)
                .ToListAsync();
            habit.Completions = completions.Select(c => new HabitCompletion
            {
                Id = Guid.Parse(c.Id),
                HabitId = Guid.Parse(c.HabitId),
                CompletedDate = DateTime.Parse(c.CompletedDateUtc),
                Note = c.Note
            }).ToList();

            return habit;
        }

        #endregion

        #region Task Operations

        public async Task<List<TodoTask>> GetAllTasksAsync()
        {
            await EnsureInitializedAsync();

            var taskDbs = await _database.Table<TaskDb>()
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var tasks = new List<TodoTask>();
            foreach (var taskDb in taskDbs)
            {
                var task = await MapToTaskAsync(taskDb);
                tasks.Add(task);
            }

            return tasks;
        }

        public async Task<TodoTask?> GetTaskByIdAsync(Guid id)
        {
            await EnsureInitializedAsync();

            var taskDb = await _database.Table<TaskDb>()
                .Where(t => t.Id == id.ToString())
                .FirstOrDefaultAsync();

            if (taskDb == null)
                return null;

            return await MapToTaskAsync(taskDb);
        }

        public async Task SaveTaskAsync(TodoTask task)
        {
            await EnsureInitializedAsync();

            var taskId = task.Id.ToString();
            var existing = await _database.Table<TaskDb>()
                .Where(t => t.Id == taskId)
                .FirstOrDefaultAsync();

            var taskDb = MapToTaskDb(task);

            // Set DisplayOrder only for new tasks
            if (existing == null)
            {
                var maxOrder = await _database.Table<TaskDb>()
                    .OrderByDescending(t => t.DisplayOrder)
                    .FirstOrDefaultAsync();
                taskDb.DisplayOrder = maxOrder?.DisplayOrder + 1 ?? 0;
            }
            else
            {
                taskDb.DisplayOrder = existing.DisplayOrder;
            }

            await _database.RunInTransactionAsync((conn) =>
            {
                // Save or update task
                if (existing != null)
                {
                    conn.Update(taskDb);
                }
                else
                {
                    conn.Insert(taskDb);
                }

                // Update subtasks (delete all + insert new)
                conn.Execute("DELETE FROM SubTasks WHERE ParentTaskId = ?", taskId);
                foreach (var subTask in task.SubTasks)
                {
                    conn.Insert(new SubTaskDb
                    {
                        Id = subTask.Id.ToString(),
                        ParentTaskId = taskId,
                        Name = subTask.Name,
                        IsCompleted = subTask.IsCompleted,
                        DisplayOrder = subTask.DisplayOrder
                    });
                }
            });
        }

        public async Task DeleteTaskAsync(Guid id)
        {
            await EnsureInitializedAsync();

            var taskId = id.ToString();

            await _database.RunInTransactionAsync((conn) =>
            {
                // Cascade delete subtasks
                conn.Execute("DELETE FROM SubTasks WHERE ParentTaskId = ?", taskId);

                // Delete task
                conn.Execute("DELETE FROM Tasks WHERE Id = ?", taskId);
            });
        }

        public async Task UpdateTaskOrderAsync(List<TodoTask> tasks)
        {
            await EnsureInitializedAsync();

            await _database.RunInTransactionAsync((conn) =>
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    tasks[i].DisplayOrder = i;
                    conn.Execute(
                        "UPDATE Tasks SET DisplayOrder = ? WHERE Id = ?",
                        i,
                        tasks[i].Id.ToString());
                }
            });
        }

        public async Task ToggleTaskCompletionAsync(Guid taskId)
        {
            await EnsureInitializedAsync();

            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return;

            task.IsCompleted = !task.IsCompleted;
            task.CompletedDate = task.IsCompleted ? DateTime.Now : null;

            var taskDb = MapToTaskDb(task);
            await _database.UpdateAsync(taskDb);
        }

        public async Task ToggleSubTaskCompletionAsync(Guid taskId, Guid subTaskId)
        {
            await EnsureInitializedAsync();

            var task = await GetTaskByIdAsync(taskId);
            if (task == null) return;

            var subTask = task.SubTasks.FirstOrDefault(st => st.Id == subTaskId);
            if (subTask == null) return;

            subTask.IsCompleted = !subTask.IsCompleted;

            var subTaskIdStr = subTaskId.ToString();

            await _database.RunInTransactionAsync((conn) =>
            {
                // Update subtask
                conn.Execute(
                    "UPDATE SubTasks SET IsCompleted = ? WHERE Id = ?",
                    subTask.IsCompleted ? 1 : 0,
                    subTaskIdStr);

                // Auto-complete parent task if enabled and all subtasks are done
                if (task.AutoCompleteWithSubtasks && task.SubTaskCompletionPercentage == 100 && !task.IsCompleted)
                {
                    task.IsCompleted = true;
                    task.CompletedDate = DateTime.Now;

                    conn.Execute(
                        "UPDATE Tasks SET IsCompleted = ?, CompletedDateUtc = ? WHERE Id = ?",
                        1,
                        task.CompletedDate.Value.ToUniversalTime().ToString("o"),
                        taskId.ToString());
                }
            });
        }

        #endregion

        #region Task Mapping Methods

        /// <summary>
        /// Maps domain TodoTask model to database TaskDb model
        /// </summary>
        private TaskDb MapToTaskDb(TodoTask task)
        {
            return new TaskDb
            {
                Id = task.Id.ToString(),
                Name = task.Name,
                Description = task.Description,
                CreatedDateUtc = task.CreatedDate.ToUniversalTime().ToString("o"),
                DueDateUtc = task.DueDate?.ToUniversalTime().ToString("o"),
                Priority = task.Priority,
                CompletedDateUtc = task.CompletedDate?.ToUniversalTime().ToString("o"),
                IsCompleted = task.IsCompleted,
                HasReminders = task.HasReminders,
                ReminderTimeTicks = task.ReminderTime?.Ticks,
                DisplayOrder = task.DisplayOrder,
                AutoCompleteWithSubtasks = task.AutoCompleteWithSubtasks
            };
        }

        /// <summary>
        /// Maps database TaskDb model to domain TodoTask model
        /// Loads related subtasks and restores parent references
        /// </summary>
        private async Task<TodoTask> MapToTaskAsync(TaskDb taskDb)
        {
            var task = new TodoTask
            {
                Id = Guid.Parse(taskDb.Id),
                Name = taskDb.Name,
                Description = taskDb.Description,
                CreatedDate = DateTime.Parse(taskDb.CreatedDateUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                DueDate = string.IsNullOrEmpty(taskDb.DueDateUtc)
                    ? null
                    : DateTime.Parse(taskDb.DueDateUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                Priority = taskDb.Priority,
                CompletedDate = string.IsNullOrEmpty(taskDb.CompletedDateUtc)
                    ? null
                    : DateTime.Parse(taskDb.CompletedDateUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                IsCompleted = taskDb.IsCompleted,
                HasReminders = taskDb.HasReminders,
                ReminderTime = taskDb.ReminderTimeTicks.HasValue
                    ? TimeSpan.FromTicks(taskDb.ReminderTimeTicks.Value)
                    : null,
                DisplayOrder = taskDb.DisplayOrder,
                AutoCompleteWithSubtasks = taskDb.AutoCompleteWithSubtasks
            };

            // Load subtasks
            var subTaskDbs = await _database.Table<SubTaskDb>()
                .Where(st => st.ParentTaskId == taskDb.Id)
                .OrderBy(st => st.DisplayOrder)
                .ToListAsync();

            task.SubTasks = subTaskDbs.Select(st => MapToSubTask(st, task)).ToList();

            return task;
        }

        /// <summary>
        /// Maps database SubTaskDb model to domain SubTask model
        /// CRITICAL: Always sets parent task reference for INotifyPropertyChanged
        /// </summary>
        private SubTask MapToSubTask(SubTaskDb subTaskDb, TodoTask parentTask)
        {
            var subTask = new SubTask
            {
                Id = Guid.Parse(subTaskDb.Id),
                ParentTaskId = Guid.Parse(subTaskDb.ParentTaskId),
                Name = subTaskDb.Name,
                IsCompleted = subTaskDb.IsCompleted,
                DisplayOrder = subTaskDb.DisplayOrder
            };

            // CRITICAL: Must call SetParentTask for INotifyPropertyChanged to work
            subTask.SetParentTask(parentTask);

            return subTask;
        }

        #endregion

        #region Statistics Operations (Stubs)

        public async Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId)
        {
            await EnsureInitializedAsync();
            throw new NotImplementedException();
        }

        public async Task<List<HabitStatistics>> GetAllHabitStatisticsAsync()
        {
            await EnsureInitializedAsync();
            throw new NotImplementedException();
        }

        public async Task<TaskStatistics> GetTaskStatisticsAsync()
        {
            await EnsureInitializedAsync();
            throw new NotImplementedException();
        }

        #endregion
    }
}
