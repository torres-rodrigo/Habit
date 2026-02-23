# SQLite Persistence Implementation

## Overview

The Tracker application uses SQLite for data persistence, providing reliable storage that persists across app restarts. This document describes the implementation details and architecture.

## Architecture

```
ViewModels (unchanged)
    ↓ (inject IDataService)
DatabaseService (implements IDataService)
    ├── SQLiteAsyncConnection
    ├── Async initialization
    ├── Transaction support
    ├── UTC DateTime handling
    ├── Mapping logic
    └── Statistics computation
    ↓
SQLite Database (tracker.db3)
    ├── 6 tables
    ├── Indexes
    └── Version tracking
```

## Database Schema

### Tables

#### Habits
- **Id** (TEXT, PRIMARY KEY) - Guid stored as string
- **Name** (TEXT) - Habit name
- **Description** (TEXT) - Habit description
- **TrackEveryday** (INTEGER/BOOL) - Track daily vs specific days
- **CreatedDateUtc** (TEXT) - ISO 8601 format, UTC
- **DeadlineUtc** (TEXT, nullable) - ISO 8601 format, UTC
- **HasReminders** (INTEGER/BOOL) - Reminder enabled flag
- **ReminderTimeTicks** (INTEGER, nullable) - TimeSpan as ticks
- **NotesEnabled** (INTEGER/BOOL) - Notes feature enabled
- **DisplayOrder** (INTEGER) - Sort order

#### HabitTrackingDays (Junction Table)
- **Id** (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- **HabitId** (TEXT, INDEXED) - Foreign key to Habits
- **DayOfWeek** (INTEGER) - 0-6 (Sunday-Saturday)

#### HabitCompletions
- **Id** (TEXT, PRIMARY KEY) - Guid stored as string
- **HabitId** (TEXT, INDEXED) - Foreign key to Habits
- **CompletedDateUtc** (TEXT, INDEXED) - Date in "yyyy-MM-dd" format
- **Note** (TEXT, nullable) - Optional completion note

#### Tasks
- **Id** (TEXT, PRIMARY KEY) - Guid stored as string
- **Name** (TEXT) - Task name
- **Description** (TEXT) - Task description
- **CreatedDateUtc** (TEXT) - ISO 8601 format, UTC
- **DueDateUtc** (TEXT, nullable) - ISO 8601 format, UTC
- **Priority** (TEXT, nullable) - Priority level
- **CompletedDateUtc** (TEXT, nullable) - ISO 8601 format, UTC
- **IsCompleted** (INTEGER/BOOL) - Completion status
- **HasReminders** (INTEGER/BOOL) - Reminder enabled flag
- **ReminderTimeTicks** (INTEGER, nullable) - TimeSpan as ticks
- **DisplayOrder** (INTEGER) - Sort order
- **AutoCompleteWithSubtasks** (INTEGER/BOOL) - Auto-complete feature

#### SubTasks
- **Id** (TEXT, PRIMARY KEY) - Guid stored as string
- **ParentTaskId** (TEXT, INDEXED) - Foreign key to Tasks
- **Name** (TEXT) - Subtask name
- **IsCompleted** (INTEGER/BOOL) - Completion status
- **DisplayOrder** (INTEGER) - Sort order

#### DatabaseInfo (Metadata)
- **Key** (TEXT, PRIMARY KEY) - Metadata key
- **Value** (TEXT) - Metadata value

### Indexes

Performance indexes created on:
- `HabitTrackingDays.HabitId`
- `HabitCompletions.HabitId`
- `HabitCompletions.CompletedDateUtc`
- `SubTasks.ParentTaskId`

## Key Implementation Details

### Async Initialization Pattern

The `DatabaseService` uses a task-based initialization pattern to avoid blocking the constructor:

```csharp
private readonly Task _initializationTask;

public DatabaseService()
{
    _initializationTask = InitializeDatabaseAsync();
}

private async Task EnsureInitializedAsync()
{
    await _initializationTask;
}
```

Every public method calls `EnsureInitializedAsync()` before executing.

### DateTime Handling

**Storage (UTC):**
- All DateTime values stored in UTC using ISO 8601 format (`"o"`)
- Date-only values use `"yyyy-MM-dd"` format
- TimeSpan values stored as ticks (long)

**Retrieval (Local Time):**
```csharp
CreatedDate = DateTime.Parse(db.CreatedDateUtc, null,
    DateTimeStyles.RoundtripKind).ToLocalTime()
```

This ensures correct handling across timezones and daylight saving time.

### Transaction Pattern

Multi-table operations use transactions to ensure data integrity:

```csharp
await _database.RunInTransactionAsync((conn) =>
{
    conn.Update(record);
    conn.Execute("DELETE FROM ...", id);
    conn.Insert(newRecord);
});
```

**Important:** Inside `RunInTransactionAsync`, use synchronous methods:
- `conn.Insert()` NOT `conn.InsertAsync()`
- `conn.Update()` NOT `conn.UpdateAsync()`
- `conn.Execute()` NOT `conn.ExecuteAsync()`

### Junction Table Pattern

TrackingDays use a full replacement pattern:

```csharp
// Delete all existing
conn.Execute("DELETE FROM HabitTrackingDays WHERE HabitId = ?", habitId);

// Insert new ones
foreach (var day in habit.TrackingDays)
{
    conn.Insert(new HabitTrackingDayDb
    {
        HabitId = habitId,
        DayOfWeek = (int)day
    });
}
```

### Parent Reference Restoration

**Critical for SubTask INotifyPropertyChanged:**

```csharp
private SubTask MapToSubTask(SubTaskDb db, TodoTask parent)
{
    var subTask = new SubTask { /* properties */ };

    // MUST call this for property change notifications to work!
    subTask.SetParentTask(parent);

    return subTask;
}
```

Always call `SetParentTask()` after loading subtasks from database.

### DisplayOrder Management

DisplayOrder is only set for **new** items:

```csharp
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
```

Use `UpdateHabitOrderAsync()` for reordering.

## Database Location

The database file `tracker.db3` is stored in:
```
Environment.SpecialFolder.LocalApplicationData
```

**Windows:** `C:\Users\{username}\AppData\Local\{appname}\tracker.db3`

## Sample Data

On first run, the database is seeded with sample data:
- 2 habits (Morning Exercise, Reading)
- 7 completions on exercise habit (alternating days)
- 2 tasks with subtasks

Sample data only loads if the database is empty (`Habits` table count = 0).

## Migration Framework

The `DatabaseInfo` table tracks the schema version. Future schema changes can be handled:

```csharp
private async Task MigrateDatabaseAsync(int fromVersion, int toVersion)
{
    if (fromVersion == 1 && toVersion >= 2)
    {
        await _database.ExecuteAsync("ALTER TABLE Habits ADD COLUMN NewField TEXT");
    }

    // Update version
    await _database.ExecuteAsync(
        "UPDATE DatabaseInfo SET Value = ? WHERE Key = 'Version'",
        toVersion.ToString());
}
```

## Statistics Computation

Statistics are computed **on-demand**, NOT stored in the database. This ensures they're always current and simplifies the data model.

Methods:
- `GetHabitStatisticsAsync()` - Single habit statistics
- `GetAllHabitStatisticsAsync()` - All habits
- `GetTaskStatisticsAsync()` - Task statistics

Statistics include:
- Completion rates
- Streaks (current and longest)
- Yearly breakdowns
- Target vs actual comparisons

## Testing

### Switching Between Implementations

In `MauiProgram.cs`:

```csharp
// Production (SQLite persistence)
builder.Services.AddSingleton<IDataService, DatabaseService>();

// Testing (in-memory, no persistence)
builder.Services.AddSingleton<IDataService, InMemoryDataService>();
```

### Manual Testing Checklist

1. **Fresh Install**
   - ✅ Sample data appears on first launch
   - ✅ Database file created in LocalApplicationData

2. **Habit CRUD**
   - ✅ Create habit → persists after restart
   - ✅ Edit habit → changes persist
   - ✅ Delete habit → removed after restart
   - ✅ Reorder habits → order persists

3. **Habit Completions**
   - ✅ Toggle completion → persists after restart
   - ✅ Add note → note persists
   - ✅ Completions show in statistics

4. **Task CRUD**
   - ✅ Create task → persists after restart
   - ✅ Create task with subtasks → all persist
   - ✅ Edit task → changes persist
   - ✅ Delete task → subtasks also deleted (cascade)

5. **SubTask Operations**
   - ✅ Toggle subtask → parent task updates
   - ✅ Auto-complete works when all subtasks done
   - ✅ Parent references maintained (no null reference errors)

6. **Statistics**
   - ✅ Habit statistics calculate correctly
   - ✅ Task statistics calculate correctly
   - ✅ Yearly breakdowns show historical data

7. **DateTime Handling**
   - ✅ Dates display in local time
   - ✅ Completions stored correctly (no off-by-one errors)
   - ✅ Reminders use local time

## Backup and Recovery

To backup your data:
1. Locate the database file in LocalApplicationData
2. Copy `tracker.db3` to a safe location

To restore:
1. Close the app
2. Replace `tracker.db3` with your backup
3. Restart the app

## Dependencies

- **sqlite-net-pcl** (1.9.172) - SQLite ORM for .NET
- **SQLitePCLRaw.bundle_green** (2.1.10) - SQLite native bindings

## Troubleshooting

### Data Not Persisting
- Check database file exists in LocalApplicationData
- Verify `DatabaseService` is registered in DI (not `InMemoryDataService`)
- Check for exceptions during database initialization

### Parent Reference Errors
- Ensure `SetParentTask()` is called after loading subtasks
- Check `MapToSubTask()` calls in `GetTaskByIdAsync()` and `GetAllTasksAsync()`

### Transaction Failures
- Verify using synchronous methods inside `RunInTransactionAsync`
- Check for proper error handling
- Ensure transaction rollback on exceptions

### DateTime Issues
- Verify UTC conversion on save: `ToUniversalTime().ToString("o")`
- Verify local conversion on load: `ToLocalTime()`
- Use `.Date` for date-only comparisons

## Performance Considerations

- Indexes created on frequently queried columns
- Transactions used for bulk operations
- Statistics computed on-demand (not on every database access)
- Junction table queries optimized with indexes

## Future Enhancements

Potential improvements:
- Export/import functionality
- Cloud sync support
- Database compaction/vacuum
- Query result caching
- Batch operations API
- Database backup automation
