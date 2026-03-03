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
        private const int CurrentDatabaseVersion = 6;

        public DatabaseService()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "tracker.db");

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
            await _database.CreateTableAsync<HabitNoteDb>();
            await _database.CreateTableAsync<HabitTrackingPeriodDb>();
            await _database.CreateTableAsync<HabitTrackingPeriodDayDb>();
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
                "CREATE INDEX IF NOT EXISTS idx_habit_notes_habit_id ON HabitNotes(HabitId)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_habit_notes_date ON HabitNotes(DateUtc)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_tracking_periods_habit_id ON HabitTrackingPeriods(HabitId)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_tracking_period_days_period_id ON HabitTrackingPeriodDays(PeriodId)");
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
            if (fromVersion < 6)
            {
                // Version 6: Add HabitTrackingPeriods - seed initial period for every existing habit
                var habits = await _database.Table<HabitDb>().ToListAsync();
                foreach (var habitDb in habits)
                {
                    var trackingDays = await _database.Table<HabitTrackingDayDb>()
                        .Where(t => t.HabitId == habitDb.Id)
                        .ToListAsync();

                    var createdDate = DateTime.Parse(habitDb.CreatedDateUtc, null,
                        DateTimeStyles.RoundtripKind).ToLocalTime();

                    var periodId = Guid.NewGuid().ToString();

                    await _database.InsertAsync(new HabitTrackingPeriodDb
                    {
                        Id = periodId,
                        HabitId = habitDb.Id,
                        StartDateUtc = createdDate.Date.ToString("yyyy-MM-dd"),
                        EndDateUtc = null,
                        TrackEveryday = habitDb.TrackEveryday
                    });

                    foreach (var day in trackingDays)
                    {
                        await _database.InsertAsync(new HabitTrackingPeriodDayDb
                        {
                            PeriodId = periodId,
                            DayOfWeek = day.DayOfWeek
                        });
                    }
                }
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

            // Batch-load all data in parallel to avoid N+1 queries
            var habitDbs = await _database.Table<HabitDb>()
                .OrderBy(h => h.DisplayOrder)
                .ToListAsync();

            var allTrackingDays = await _database.Table<HabitTrackingDayDb>().ToListAsync();
            var allCompletions = await _database.Table<HabitCompletionDb>().ToListAsync();
            var allPeriods = await _database.Table<HabitTrackingPeriodDb>().ToListAsync();
            var allPeriodDays = await _database.Table<HabitTrackingPeriodDayDb>().ToListAsync();

            // Group by foreign keys for O(1) lookup
            var trackingDaysByHabit = allTrackingDays.ToLookup(t => t.HabitId);
            var completionsByHabit = allCompletions.ToLookup(c => c.HabitId);
            var periodsByHabit = allPeriods.ToLookup(p => p.HabitId);
            var periodDaysByPeriod = allPeriodDays.ToLookup(d => d.PeriodId);

            var habits = new List<Habit>();
            foreach (var habitDb in habitDbs)
            {
                var habit = MapToHabit(habitDb, trackingDaysByHabit, completionsByHabit, periodsByHabit, periodDaysByPeriod);
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

                // Handle tracking periods
                if (existing == null)
                {
                    // NEW HABIT: Create initial period from creation date
                    var periodId = Guid.NewGuid().ToString();
                    conn.Insert(new HabitTrackingPeriodDb
                    {
                        Id = periodId,
                        HabitId = habitId,
                        StartDateUtc = habit.CreatedDate.Date.ToString("yyyy-MM-dd"),
                        EndDateUtc = null,
                        TrackEveryday = habit.TrackEveryday
                    });

                    foreach (var day in habit.TrackingDays)
                    {
                        conn.Insert(new HabitTrackingPeriodDayDb
                        {
                            PeriodId = periodId,
                            DayOfWeek = (int)day
                        });
                    }
                }
                else
                {
                    // EXISTING HABIT: Check if tracking config changed
                    var activePeriods = conn.Table<HabitTrackingPeriodDb>()
                        .Where(p => p.HabitId == habitId)
                        .ToList()
                        .Where(p => p.EndDateUtc == null)
                        .ToList();

                    if (activePeriods.Count > 0)
                    {
                        var activePeriod = activePeriods.First();
                        var configChanged = false;

                        if (activePeriod.TrackEveryday != habit.TrackEveryday)
                        {
                            configChanged = true;
                        }
                        else if (!habit.TrackEveryday)
                        {
                            var existingPeriodDays = conn.Table<HabitTrackingPeriodDayDb>()
                                .Where(d => d.PeriodId == activePeriod.Id)
                                .ToList()
                                .Select(d => d.DayOfWeek)
                                .OrderBy(d => d)
                                .ToList();

                            var newDays = habit.TrackingDays
                                .Select(d => (int)d)
                                .OrderBy(d => d)
                                .ToList();

                            configChanged = !existingPeriodDays.SequenceEqual(newDays);
                        }

                        if (configChanged)
                        {
                            var today = DateTime.Today.ToString("yyyy-MM-dd");

                            if (activePeriod.StartDateUtc == today)
                            {
                                // Period started today - update in place
                                activePeriod.TrackEveryday = habit.TrackEveryday;
                                conn.Update(activePeriod);

                                conn.Execute("DELETE FROM HabitTrackingPeriodDays WHERE PeriodId = ?",
                                    activePeriod.Id);
                                foreach (var day in habit.TrackingDays)
                                {
                                    conn.Insert(new HabitTrackingPeriodDayDb
                                    {
                                        PeriodId = activePeriod.Id,
                                        DayOfWeek = (int)day
                                    });
                                }
                            }
                            else
                            {
                                // Close active period (end = yesterday)
                                var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                                activePeriod.EndDateUtc = yesterday;
                                conn.Update(activePeriod);

                                // Create new period starting today
                                var newPeriodId = Guid.NewGuid().ToString();
                                conn.Insert(new HabitTrackingPeriodDb
                                {
                                    Id = newPeriodId,
                                    HabitId = habitId,
                                    StartDateUtc = today,
                                    EndDateUtc = null,
                                    TrackEveryday = habit.TrackEveryday
                                });

                                foreach (var day in habit.TrackingDays)
                                {
                                    conn.Insert(new HabitTrackingPeriodDayDb
                                    {
                                        PeriodId = newPeriodId,
                                        DayOfWeek = (int)day
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // No active period exists - create one defensively
                        var periodId = Guid.NewGuid().ToString();
                        conn.Insert(new HabitTrackingPeriodDb
                        {
                            Id = periodId,
                            HabitId = habitId,
                            StartDateUtc = habit.CreatedDate.Date.ToString("yyyy-MM-dd"),
                            EndDateUtc = null,
                            TrackEveryday = habit.TrackEveryday
                        });

                        foreach (var day in habit.TrackingDays)
                        {
                            conn.Insert(new HabitTrackingPeriodDayDb
                            {
                                PeriodId = periodId,
                                DayOfWeek = (int)day
                            });
                        }
                    }
                }

                // Completions are managed separately via ToggleHabitCompletionAsync —
                // not rewritten here to avoid destructive bulk delete/re-insert on every save.
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

                // Cascade delete tracking period days and periods
                var periodIds = conn.Table<HabitTrackingPeriodDb>()
                    .Where(p => p.HabitId == habitId)
                    .ToList()
                    .Select(p => p.Id)
                    .ToList();

                foreach (var periodId in periodIds)
                {
                    conn.Execute("DELETE FROM HabitTrackingPeriodDays WHERE PeriodId = ?", periodId);
                }
                conn.Execute("DELETE FROM HabitTrackingPeriods WHERE HabitId = ?", habitId);

                // Cascade delete completions
                conn.Execute("DELETE FROM HabitCompletions WHERE HabitId = ?", habitId);

                // Cascade delete notes
                conn.Execute("DELETE FROM HabitNotes WHERE HabitId = ?", habitId);

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

        public async Task UpdateHabitDisplayOrderAsync(Guid habitId, int displayOrder)
        {
            await EnsureInitializedAsync();

            await _database.ExecuteAsync(
                "UPDATE Habits SET DisplayOrder = ? WHERE Id = ?",
                displayOrder,
                habitId.ToString());
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

        public async Task<string?> GetHabitNoteAsync(Guid habitId, DateTime date)
        {
            await EnsureInitializedAsync();

            var habitIdStr = habitId.ToString();
            var dateOnly = date.Date.ToString("yyyy-MM-dd");

            var note = await _database.Table<HabitNoteDb>()
                .Where(n => n.HabitId == habitIdStr && n.DateUtc == dateOnly)
                .FirstOrDefaultAsync();

            return note?.Text;
        }

        public async Task SaveHabitNoteAsync(Guid habitId, DateTime date, string noteText)
        {
            await EnsureInitializedAsync();

            var habitIdStr = habitId.ToString();
            var dateOnly = date.Date.ToString("yyyy-MM-dd");

            var existing = await _database.Table<HabitNoteDb>()
                .Where(n => n.HabitId == habitIdStr && n.DateUtc == dateOnly)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(noteText))
            {
                // If note text is empty, remove the note if it exists
                if (existing != null)
                {
                    await _database.DeleteAsync(existing);
                }
            }
            else
            {
                if (existing != null)
                {
                    // Update existing note
                    existing.Text = noteText;
                    await _database.UpdateAsync(existing);
                }
                else
                {
                    // Add new note
                    await _database.InsertAsync(new HabitNoteDb
                    {
                        Id = Guid.NewGuid().ToString(),
                        HabitId = habitIdStr,
                        DateUtc = dateOnly,
                        Text = noteText
                    });
                }
            }
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
                IsTracked = habit.IsTracked,
                UntrackedDateUtc = habit.UntrackedDate?.ToUniversalTime().ToString("o"),
                DisplayOrder = habit.DisplayOrder
            };
        }

        /// <summary>
        /// Maps database HabitDb model to domain Habit model using pre-loaded related data.
        /// Used by GetAllHabitsAsync to avoid N+1 queries.
        /// </summary>
        private Habit MapToHabit(
            HabitDb habitDb,
            ILookup<string, HabitTrackingDayDb> trackingDaysByHabit,
            ILookup<string, HabitCompletionDb> completionsByHabit,
            ILookup<string, HabitTrackingPeriodDb> periodsByHabit,
            ILookup<string, HabitTrackingPeriodDayDb> periodDaysByPeriod)
        {
            var habit = MapHabitDbFields(habitDb);

            habit.TrackingDays = trackingDaysByHabit[habitDb.Id]
                .Select(d => (DayOfWeek)d.DayOfWeek).ToList();

            habit.Completions = completionsByHabit[habitDb.Id]
                .Select(c => new HabitCompletion
                {
                    Id = Guid.Parse(c.Id),
                    HabitId = Guid.Parse(c.HabitId),
                    CompletedDate = DateTime.Parse(c.CompletedDateUtc),
                    Note = c.Note
                }).ToList();

            habit.TrackingPeriods = periodsByHabit[habitDb.Id]
                .Select(periodDb => new HabitTrackingPeriod
                {
                    Id = Guid.Parse(periodDb.Id),
                    HabitId = Guid.Parse(periodDb.HabitId),
                    StartDate = DateTime.Parse(periodDb.StartDateUtc),
                    EndDate = string.IsNullOrEmpty(periodDb.EndDateUtc)
                        ? null
                        : DateTime.Parse(periodDb.EndDateUtc),
                    TrackEveryday = periodDb.TrackEveryday,
                    TrackingDays = periodDaysByPeriod[periodDb.Id]
                        .Select(d => (DayOfWeek)d.DayOfWeek).ToList()
                })
                .OrderBy(p => p.StartDate)
                .ToList();

            return habit;
        }

        /// <summary>
        /// Maps database HabitDb model to domain Habit model.
        /// Loads related data via individual queries — used for single-item lookups.
        /// </summary>
        private async Task<Habit> MapToHabitAsync(HabitDb habitDb)
        {
            var habit = MapHabitDbFields(habitDb);

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

            // Load tracking periods
            var periods = await _database.Table<HabitTrackingPeriodDb>()
                .Where(p => p.HabitId == habitDb.Id)
                .ToListAsync();

            habit.TrackingPeriods = new List<HabitTrackingPeriod>();
            foreach (var periodDb in periods)
            {
                var periodDays = await _database.Table<HabitTrackingPeriodDayDb>()
                    .Where(d => d.PeriodId == periodDb.Id)
                    .ToListAsync();

                habit.TrackingPeriods.Add(new HabitTrackingPeriod
                {
                    Id = Guid.Parse(periodDb.Id),
                    HabitId = Guid.Parse(periodDb.HabitId),
                    StartDate = DateTime.Parse(periodDb.StartDateUtc),
                    EndDate = string.IsNullOrEmpty(periodDb.EndDateUtc)
                        ? null
                        : DateTime.Parse(periodDb.EndDateUtc),
                    TrackEveryday = periodDb.TrackEveryday,
                    TrackingDays = periodDays.Select(d => (DayOfWeek)d.DayOfWeek).ToList()
                });
            }

            habit.TrackingPeriods = habit.TrackingPeriods.OrderBy(p => p.StartDate).ToList();

            return habit;
        }

        /// <summary>
        /// Maps only the scalar fields from HabitDb to Habit (no related data).
        /// Shared by both MapToHabit and MapToHabitAsync.
        /// </summary>
        private Habit MapHabitDbFields(HabitDb habitDb)
        {
            return new Habit
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
                IsTracked = habitDb.IsTracked,
                UntrackedDate = string.IsNullOrEmpty(habitDb.UntrackedDateUtc)
                    ? null
                    : DateTime.Parse(habitDb.UntrackedDateUtc, null, DateTimeStyles.RoundtripKind).ToLocalTime(),
                DisplayOrder = habitDb.DisplayOrder
            };
        }

        #endregion

        #region Task Operations

        public async Task<List<TodoTask>> GetAllTasksAsync()
        {
            await EnsureInitializedAsync();

            // Batch-load all data to avoid N+1 queries
            var taskDbs = await _database.Table<TaskDb>()
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var allSubTasks = await _database.Table<SubTaskDb>()
                .OrderBy(st => st.DisplayOrder)
                .ToListAsync();

            var subTasksByParent = allSubTasks.ToLookup(st => st.ParentTaskId);

            var tasks = new List<TodoTask>();
            foreach (var taskDb in taskDbs)
            {
                var task = MapToTask(taskDb, subTasksByParent);
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
                AutoCompleteWithSubtasks = task.AutoCompleteWithSubtasks,
                IsPinned = task.IsPinned
            };
        }

        /// <summary>
        /// Maps database TaskDb model to domain TodoTask model using pre-loaded subtasks.
        /// Used by GetAllTasksAsync to avoid N+1 queries.
        /// </summary>
        private TodoTask MapToTask(TaskDb taskDb, ILookup<string, SubTaskDb> subTasksByParent)
        {
            var task = MapTaskDbFields(taskDb);
            task.SubTasks = subTasksByParent[taskDb.Id]
                .OrderBy(st => st.DisplayOrder)
                .Select(st => MapToSubTask(st, task))
                .ToList();
            return task;
        }

        /// <summary>
        /// Maps database TaskDb model to domain TodoTask model.
        /// Loads subtasks via individual query — used for single-item lookups.
        /// </summary>
        private async Task<TodoTask> MapToTaskAsync(TaskDb taskDb)
        {
            var task = MapTaskDbFields(taskDb);

            // Load subtasks
            var subTaskDbs = await _database.Table<SubTaskDb>()
                .Where(st => st.ParentTaskId == taskDb.Id)
                .OrderBy(st => st.DisplayOrder)
                .ToListAsync();

            task.SubTasks = subTaskDbs.Select(st => MapToSubTask(st, task)).ToList();

            return task;
        }

        /// <summary>
        /// Maps only the scalar fields from TaskDb to TodoTask (no related data).
        /// Shared by both MapToTask and MapToTaskAsync.
        /// </summary>
        private TodoTask MapTaskDbFields(TaskDb taskDb)
        {
            return new TodoTask
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
                AutoCompleteWithSubtasks = taskDb.AutoCompleteWithSubtasks,
                IsPinned = taskDb.IsPinned
            };
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

            return StatisticsCalculator.CalculateHabitStatistics(habit);
        }

        public async Task<List<HabitStatistics>> GetAllHabitStatisticsAsync()
        {
            await EnsureInitializedAsync();

            var habits = await GetAllHabitsAsync();
            return StatisticsCalculator.CalculateAllHabitStatistics(habits);
        }

        public async Task<TaskStatistics> GetTaskStatisticsAsync()
        {
            await EnsureInitializedAsync();

            var tasks = await GetAllTasksAsync();
            return StatisticsCalculator.CalculateTaskStatistics(tasks);
        }

        #endregion
    }
}
