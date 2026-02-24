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
        private const int CurrentDatabaseVersion = 2;

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
                // New database - set version and seed sample data
                await _database.InsertAsync(new DatabaseInfoDb
                {
                    Key = "Version",
                    Value = CurrentDatabaseVersion.ToString()
                });

                // Seed sample data on first run
                await SeedSampleDataAsync();
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
            // Migration from version 1 to 2: Add IsNegativeHabit column
            if (fromVersion == 1 && toVersion >= 2)
            {
                await _database.ExecuteAsync("ALTER TABLE Habits ADD COLUMN IsNegativeHabit INTEGER NOT NULL DEFAULT 0");
            }

            // Update version
            await _database.ExecuteAsync(
                "UPDATE DatabaseInfo SET Value = ? WHERE Key = 'Version'",
                toVersion.ToString());
        }

        /// <summary>
        /// Seeds sample data for new installations
        /// Only runs if database is empty
        /// </summary>
        private async Task SeedSampleDataAsync()
        {
            // Check if database already has data
            var habitCount = await _database.Table<HabitDb>().CountAsync();
            if (habitCount > 0)
                return;

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

            await SaveHabitAsync(exerciseHabit);
            await SaveHabitAsync(readingHabit);

            // Add some sample completions to the exercise habit
            var today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-i);
                if (i % 2 == 0)
                {
                    await ToggleHabitCompletionAsync(
                        exerciseHabit.Id,
                        date,
                        i == 0 ? "Felt great today!" : null);
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

            // Set parent references for subtasks
            foreach (var subTask in task1.SubTasks)
            {
                subTask.ParentTaskId = task1.Id;
                subTask.SetParentTask(task1);
            }

            await SaveTaskAsync(task1);
            await SaveTaskAsync(task2);
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

            var idString = id.ToString();
            var habitDb = await _database.Table<HabitDb>()
                .Where(h => h.Id == idString)
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
                IsNegativeHabit = habit.IsNegativeHabit,
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
                IsNegativeHabit = habitDb.IsNegativeHabit,
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

            var idString = id.ToString();
            var taskDb = await _database.Table<TaskDb>()
                .Where(t => t.Id == idString)
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

        #region Statistics Operations

        public async Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId)
        {
            await EnsureInitializedAsync();

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

            // Calculate all-time expected completions
            var allTimeExpected = 0;
            var date = habit.CreatedDate.Date;
            while (date <= today)
            {
                if (ShouldTrackOnDay(habit, date))
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
                    if (ShouldTrackOnDay(habit, currentDate))
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
                AllTimeCompletions = allTimeCompletions,
                AllTimeExpected = allTimeExpected,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                CompletionRate = Math.Round(completionRate, 1),
                YearlyBreakdown = yearlyBreakdown
            };
        }

        public async Task<List<HabitStatistics>> GetAllHabitStatisticsAsync()
        {
            await EnsureInitializedAsync();

            var statistics = new List<HabitStatistics>();
            var habits = await GetAllHabitsAsync();

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

        public async Task<TaskStatistics> GetTaskStatisticsAsync()
        {
            await EnsureInitializedAsync();

            var tasks = await GetAllTasksAsync();

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
                LateRate = Math.Round(lateRate, 1),
                YearlyBreakdown = yearlyBreakdown
            };

            return statistics;
        }

        #endregion

        #region Statistics Helper Methods

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

        #endregion
    }
}
