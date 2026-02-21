# Tracker - Habit & Task Manager

A comprehensive .NET MAUI app for tracking habits and managing tasks across multiple platforms (Windows, macOS, iOS, Android).

## Features

### 📊 Statistics Tab
- **Habit Statistics**: View daily, weekly, monthly, and yearly progress for all habits
- **Task Statistics**: Track total tasks, completion rates, on-time completion, and overdue tasks
- **Progress Visualization**: Progress bars and completion percentages
- **Streak Tracking**: Current and longest streaks for each habit

### ✅ Habits Tab
- **Habit Cards**: Visual cards displaying habit name and current week's progress
- **Quick Completion**: Tap on any day to mark habit as completed
- **Customizable Tracking**: 
  - Track everyday or specific days of the week
  - Set deadlines or track indefinitely
  - Enable notes for specific days
  - Configure reminders
- **Detailed View**: Tap any habit to view detailed statistics and edit settings

### 📝 Tasks Tab
- **Two Sections**: 
  - **To-Do**: Active tasks requiring completion
  - **Completed**: Finished tasks with completion dates
- **Subtasks**: Add subtasks that auto-complete parent task when all are done
- **Due Dates**: Optional deadlines with tracking of on-time vs late completion
- **Reminders**: Set custom reminder times
- **Quick Toggle**: Tap checkbox to complete/uncomplete tasks

### 🎨 UI Features
- **Floating Action Button**: Quick access to create new habits or tasks
- **Bottom Navigation**: Easy switching between Statistics, Habits, and Tasks
- **Modern Design**: Clean interface with progress indicators and visual feedback
- **Scrollable Views**: Handle large numbers of habits and tasks efficiently

## Project Structure

```
Tracker/
├── Models/
│   ├── Habit.cs                    # Habit entity with tracking settings
│   ├── TodoTask.cs                 # Task and SubTask entities
│   └── HabitStatistics.cs          # Statistics models
├── ViewModels/
│   ├── BaseViewModel.cs            # Base class with INotifyPropertyChanged
│   ├── StatisticsViewModel.cs     # Statistics tab logic
│   ├── HabitViewModel.cs          # Habits tab logic
│   ├── TaskViewModel.cs           # Tasks tab logic
│   ├── EditHabitViewModel.cs      # Edit/Create habit logic
│   ├── EditTaskViewModel.cs       # Edit/Create task logic
│   └── SelectItemTypeViewModel.cs # Modal popup for selecting item type
├── Views/
│   ├── StatisticsPage.xaml        # Statistics UI
│   ├── HabitsPage.xaml            # Habits UI
│   ├── TasksPage.xaml             # Tasks UI
│   ├── EditHabitPage.xaml         # Edit/Create habit UI
│   ├── EditTaskPage.xaml          # Edit/Create task UI
│   └── SelectItemTypePage.xaml    # Modal popup UI
├── Services/
│   └── DataService.cs             # Data management and business logic
├── Converters/
│   └── ValueConverters.cs         # XAML value converters
├── App.xaml                       # Application resources
├── AppShell.xaml                  # Navigation structure
└── MauiProgram.cs                 # Dependency injection setup
```

## Detailed File Descriptions

### Models (`Models/`)

#### `Habit.cs`
Defines the Habit entity that represents a trackable habit.
- **Properties**: Id, Name, Description, TrackEveryday, TrackingDays (collection of DayOfWeek), Deadline, HasReminders, ReminderTime, NotesEnabled, CompletionHistory (dictionary of date -> completion status)
- **Purpose**: Core data model for habits with all configuration and tracking data
- **Usage**: Used by DataService, HabitViewModel, EditHabitViewModel, and StatisticsViewModel

#### `TodoTask.cs`
Defines the TodoTask and SubTask entities for task management.
- **TodoTask Properties**: Id, Name, Description, IsCompleted, DueDate, CompletedDate, HasReminders, ReminderTime, SubTasks (collection), DisplayOrder, Priority, CreatedDate
- **SubTask Properties**: Id, ParentTaskId, Name, IsCompleted, DisplayOrder
- **Purpose**: Represents tasks with optional subtasks and completion tracking
- **Usage**: Used by DataService, TaskViewModel, and EditTaskViewModel

#### `HabitStatistics.cs`
Defines statistics models for habit tracking metrics.
- **HabitStatisticsModel Properties**: HabitName, DailyProgress, WeeklyProgress, MonthlyProgress, YearlyProgress, CurrentStreak, LongestStreak
- **TaskStatisticsModel Properties**: TotalTasks, CompletedTasks, CompletionRate, OnTimeCompletions, LateCompletions, OverdueTasks
- **Purpose**: Aggregated statistics for displaying progress and insights
- **Usage**: Used by StatisticsViewModel to display metrics

### ViewModels (`ViewModels/`)

#### `BaseViewModel.cs`
Abstract base class for all ViewModels implementing INotifyPropertyChanged.
- **Properties**: Title (page title), IsBusy (loading state)
- **Methods**: SetProperty (property change notification helper), OnPropertyChanged (manual notification)
- **Purpose**: Provides common functionality for all ViewModels (property change notifications, busy states)
- **Inheritance**: Inherited by all other ViewModels

#### `StatisticsViewModel.cs`
ViewModel for the Statistics tab displaying habit and task metrics.
- **Properties**: HabitStatistics (collection of HabitStatisticsModel), TaskStatistics (TaskStatisticsModel)
- **Methods**: LoadStatistics (fetches data from DataService), Refresh (reloads data)
- **Purpose**: Manages statistics display logic and data binding for the Statistics page
- **Dependencies**: IDataService

#### `HabitViewModel.cs`
ViewModel for the Habits tab displaying habit cards with weekly progress.
- **Properties**: Habits (collection of HabitCardViewModel)
- **Commands**: AddHabitCommand (navigate to item selection), EditHabitCommand (navigate to edit), DeleteHabitCommand, ToggleCompletionCommand (mark day complete/incomplete)
- **Nested Classes**: 
  - HabitCardViewModel: Represents a single habit card with weekly progress
  - DayCompletionViewModel: Represents a single day circle with completion status
- **Purpose**: Manages habit list display and user interactions
- **Dependencies**: IDataService

#### `TaskViewModel.cs`
ViewModel for the Tasks tab displaying to-do and completed tasks.
- **Properties**: TodoTasks (active tasks), CompletedTasks (finished tasks)
- **Commands**: AddTaskCommand (navigate to item selection), EditTaskCommand (navigate to edit), DeleteTaskCommand, ToggleCompletionCommand (mark task complete/incomplete)
- **Nested Classes**: TaskItemViewModel: Represents a task with subtasks
- **Purpose**: Manages task list display with subtasks and completion tracking
- **Dependencies**: IDataService

#### `EditHabitViewModel.cs`
ViewModel for creating or editing habits.
- **Properties**: HabitIdString (receives URL parameter), Name, Description, TrackEveryday, HasDeadline, Deadline, HasReminders, ReminderTime, NotesEnabled, DaysOfWeek (collection for day selection)
- **Commands**: SaveCommand (saves habit and navigates back), CancelCommand (navigates back without saving)
- **Methods**: LoadHabit (loads existing habit for editing), OnSave (validates and saves habit)
- **Purpose**: Handles all habit creation and editing logic
- **Navigation**: Receives `id` query parameter for editing existing habits
- **Dependencies**: IDataService

#### `EditTaskViewModel.cs`
ViewModel for creating or editing tasks with subtask management.
- **Properties**: TaskIdString (receives URL parameter), Name, Description, HasDueDate, DueDate, HasReminders, ReminderTime, Subtasks (collection of SubtaskItem), Priority
- **Commands**: SaveCommand, CancelCommand, AddSubtaskCommand, RemoveSubtaskCommand, ToggleSubtaskCommand
- **Nested Classes**: SubtaskItem: Represents a subtask in the UI
- **Methods**: LoadTask (loads existing task), OnSave (validates and saves task with subtasks)
- **Purpose**: Handles task creation/editing with full subtask support
- **Navigation**: Receives `id` query parameter for editing existing tasks
- **Dependencies**: IDataService

#### `SelectItemTypeViewModel.cs`
ViewModel for the modal popup that lets users choose between creating a Habit or Task.
- **Commands**: NavigateToHabitCommand (navigate to EditHabitPage), NavigateToTaskCommand (navigate to EditTaskPage), CancelCommand (dismiss modal)
- **Purpose**: Provides navigation logic for the item type selection modal
- **Navigation**: Uses relative paths for navigation (../habits/edithabit, ../tasks/edittask)

### Views (`Views/`)

#### `StatisticsPage.xaml` / `StatisticsPage.xaml.cs`
UI for displaying habit and task statistics.
- **Layout**: ScrollView with two sections (Habit Statistics and Task Statistics)
- **Habit Section**: Displays each habit's name, current streak, longest streak, and progress bars for daily/weekly/monthly/yearly completion rates
- **Task Section**: Shows total tasks, completed tasks, completion rate, on-time vs late completions, and overdue tasks
- **Binding**: Bound to StatisticsViewModel
- **Purpose**: Visual representation of all tracking metrics

#### `HabitsPage.xaml` / `HabitsPage.xaml.cs`
UI for displaying habit cards with weekly progress tracking.
- **Layout**: Floating Action Button (FAB) + CollectionView of habit cards
- **Habit Card**: Shows habit name and 7 day circles (one per day of week) with completion status
- **Interactions**: 
  - Tap habit card to edit
  - Tap day circle to toggle completion for that day
  - Tap FAB to create new habit
- **Binding**: Bound to HabitViewModel with nested HabitCardViewModel items
- **Purpose**: Primary interface for tracking daily habit completion

#### `TasksPage.xaml` / `TasksPage.xaml.cs`
UI for displaying tasks in To-Do and Completed sections.
- **Layout**: Two sections with CollectionViews - Active tasks and Completed tasks
- **Task Item**: Shows checkbox, task name, due date, and expandable subtasks list
- **Subtask Display**: Each subtask shown with checkbox and name, strikes through when completed
- **Interactions**:
  - Tap checkbox to complete/uncomplete task
  - Tap task to edit
  - Tap FAB to create new task
- **Binding**: Bound to TaskViewModel with nested TaskItemViewModel items
- **Purpose**: Task management with hierarchical subtask display

#### `EditHabitPage.xaml` / `EditHabitPage.xaml.cs`
UI for creating or editing a habit.
- **Layout**: Form with fields for Name, Description, Tracking settings, Deadline, Reminders, and Notes
- **Tracking Options**: Toggle for "Track Everyday" and checkboxes for specific days of the week
- **Optional Fields**: Deadline date picker, Reminder time picker (shown/hidden based on toggles)
- **Actions**: Save and Cancel buttons at bottom
- **Binding**: Bound to EditHabitViewModel
- **Purpose**: Complete habit configuration interface

#### `EditTaskPage.xaml` / `EditTaskPage.xaml.cs`
UI for creating or editing a task with subtasks.
- **Layout**: Form with Name, Description, Due Date, Reminders, Priority, and Subtasks section
- **Subtasks Section**: 
  - List of existing subtasks with checkboxes and delete buttons
  - Entry field and "Add" button for new subtasks
- **Optional Fields**: Due date picker, Reminder time picker (shown/hidden based on toggles)
- **Actions**: Save and Cancel buttons
- **Binding**: Bound to EditTaskViewModel
- **Purpose**: Task creation/editing with inline subtask management

#### `SelectItemTypePage.xaml` / `SelectItemTypePage.xaml.cs`
Modal popup for selecting whether to create a Habit or Task.
- **Layout**: Semi-transparent overlay with centered modal card
- **Options**: Two large buttons - "Habit" and "Task" with icons
- **Interactions**: 
  - Tap Habit button to navigate to EditHabitPage
  - Tap Task button to navigate to EditTaskPage
  - Tap background or Cancel button to dismiss
- **Binding**: Bound to SelectItemTypeViewModel
- **Purpose**: User-friendly item type selection after clicking FAB

### Services (`Services/`)

#### `DataService.cs`
Centralized data management service implementing IDataService interface.
- **Data Storage**: In-memory lists for Habits and TodoTasks (no persistence yet)
- **Habit Methods**: 
  - GetAllHabits, GetHabitById, SaveHabit (create/update), DeleteHabit
  - ToggleHabitCompletion, IsHabitCompletedOnDate
  - CalculateHabitStatistics (computes streaks and completion rates)
- **Task Methods**:
  - GetAllTasks, GetTaskById, SaveTask (create/update), DeleteTask
  - ToggleTaskCompletion (auto-completes when all subtasks done)
  - CalculateTaskStatistics
- **Initialization**: CreateSampleData method populates initial demo data
- **Purpose**: Single source of truth for all data operations and business logic
- **Registration**: Registered as singleton in MauiProgram.cs

### Converters (`Converters/`)

#### `ValueConverters.cs`
Collection of XAML value converters for data binding transformations.
- **BoolToColorConverter**: Converts boolean to color (used for day circles - Primary when true, White when false)
- **StringNullOrEmptyConverter**: Converts string to boolean (true if null/empty)
- **NullToBoolConverter**: Converts null to boolean (true if not null)
- **CountToBoolConverter**: Converts collection count to boolean (true if count > 0)
- **BoolToTextDecorationConverter**: Converts boolean to TextDecorations (Strikethrough when true)
- **InvertedBoolConverter**: Inverts boolean value (true -> false, false -> true)
- **Purpose**: Enable complex UI logic in XAML without code-behind
- **Usage**: Registered in App.xaml ResourceDictionary, used throughout XAML views

### Application Files

#### `App.xaml` / `App.xaml.cs`
Application-level resources and initialization.
- **Resources**: Defines global color scheme (Primary, Secondary, Tertiary, Background, Text colors)
- **Value Converters**: Registers all converters in ResourceDictionary for global access
- **Lifecycle**: Sets MainPage to AppShell on startup
- **Purpose**: Application entry point and global resource definitions

#### `AppShell.xaml` / `AppShell.xaml.cs`
Navigation structure and routing configuration.
- **TabBar**: Defines 3 tabs (Statistics, Habits, Tasks) with icons and routes
- **Route Registration**: Registers navigation routes for:
  - selectitemtype (modal popup)
  - habits/edithabit (edit habit page)
  - tasks/edittask (edit task page)
- **Purpose**: Shell-based navigation structure and route mapping
- **Navigation**: Supports both tab navigation and modal push navigation

#### `MauiProgram.cs`
Dependency injection and application configuration.
- **Services**: 
  - IDataService registered as Singleton (shared instance)
- **Views**: All pages registered as Transient (new instance each navigation)
- **ViewModels**: All ViewModels registered as Transient
- **Fonts**: Registers custom fonts (OpenSans Regular/Semibold)
- **Debug Tools**: Configures logging for development
- **Purpose**: Bootstraps the application with dependency injection container

#### `Platforms/`
Platform-specific code and resources for Windows, Android, iOS, and MacCatalyst.
- **Windows**: App icons, splash screen, and Windows-specific configurations
- **Android**: AndroidManifest.xml, resources, and Android-specific code
- **iOS**: Info.plist, resources, and iOS-specific code
- **MacCatalyst**: Info.plist and macOS-specific configurations
- **Purpose**: Platform-specific entry points and resource definitions

## Technologies Used

- **.NET 9.0** with .NET MAUI
- **MVVM Pattern** for clean architecture
- **Dependency Injection** for service management
- **XAML** for declarative UI
- **C# 12** with nullable reference types

## Getting Started

### Prerequisites
- Visual Studio 2022 (17.8 or later) or Visual Studio Code
- .NET 9.0 SDK
- .NET MAUI workload installed
- Platform-specific SDKs (optional):
  - Android SDK (for Android development)
  - Xcode (for iOS/macOS development on macOS)

### Installation

1. **Clone or navigate to the project directory**
   ```bash
   cd c:\Users\rodrigo.torrescosta\Documents\LOCAL_DOCS\my_projects\tracker\Tracker
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the app**
   
   For Windows:
   ```bash
   dotnet build -t:Run -f net9.0-windows10.0.19041.0
   ```
   
   For MacCatalyst (on macOS):
   ```bash
   dotnet build -t:Run -f net9.0-maccatalyst
   ```
   
   For iOS Simulator (on macOS):
   ```bash
   dotnet build -t:Run -f net9.0-ios
   ```
   
   For Android:
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

## Current Implementation Status

### ✅ Completed Features
- [x] Three-tab navigation (Statistics, Habits, Tasks)
- [x] Statistics page with habit and task metrics
- [x] Habits page with weekly progress cards
- [x] Tasks page with To-Do/Completed sections
- [x] Floating action buttons for adding new items
- [x] Sample data for demonstration
- [x] MVVM architecture with ViewModels
- [x] Data service for business logic
- [x] Value converters for UI binding

### 🚧 Features to Implement
- [ ] Add/Edit Habit page with full configuration
- [ ] Add/Edit Task page with subtask management
- [ ] Calendar view for habit details
- [ ] Long-press context menu (Edit, Delete, Sort)
- [ ] Drag-and-drop reordering
- [ ] Persistent storage (SQLite)
- [ ] Notification system for reminders
- [ ] Notes functionality for habits
- [ ] Export/import data
- [ ] Dark mode theme
- [ ] Cloud sync support

## Sample Data

The app comes with sample data including:
- 2 sample habits (Morning Exercise, Reading)
- 2 sample tasks (Complete project report with subtasks, Buy groceries)
- Completion history for the past week

## Data Models

### Habit
- Name, Description
- Tracking days (everyday or specific weekdays)
- Optional deadline
- Reminder settings
- Notes enabled flag
- Completion history

### Task
- Name, Description
- Due date
- Completed status and date
- Subtasks collection
- Reminder settings
- Auto-completion when all subtasks are done

### Statistics
- Daily/Weekly/Monthly/Yearly progress
- Current and longest streaks
- Completion rates
- On-time vs late completion tracking

## Contributing

This is a personal project, but suggestions and improvements are welcome!

## License

This project is provided as-is for personal use.

## Future Enhancements

- **Gamification**: Points, badges, and achievements
- **Analytics**: Detailed insights and trends
- **Social Features**: Share progress with friends
- **Widgets**: Home screen widgets for quick access
- **Apple Watch/Android Wear**: Companion apps
- **Backup & Restore**: Cloud backup integration
- **Multiple Themes**: Customizable color schemes
- **Localization**: Multi-language support

## Support

For issues or questions, please create an issue in the project repository.

---

**Built with ❤️ using .NET MAUI**

PERSONAL TODO

Notifications/reminders

quick double click should cause like a failed case insted of success

Move tab selection to bottom and center
Have tab sections be collapsable
On the statisctics tab have a filter for all time, and current year

Add way to add note for the current day in the card for the habit
it should have a revert to revert to the way the card was pre save
it should have a save button

Do a proper display page and fix the edit page

Persistance

HABITS THAT END SHOULD HAVE A SECTION COMPLETED HABITS
THere should be a toggle to show or hide completed habits

Filters
- Statistics should have an all time and current year

- Tasks should have an all time, current year, current month, current week, current day
  (this is to see what you completed in this time)
  Seperate priority filter
  Due date

  Clicking a completed task should ask the user if they are sure they want to mark it as incomplete
  Are you sure you want to edit a completed task

  Out of date comment for taks

slightly reduce the size of the subtask checkbox
reducing spacing between sub task check box and text
reduce spacing between subtasks
put a limit to the lenght of the name of a subtask
Have a maximum of 5 or seven subtasks that display if more scorll
When there is enough subtasks to scroll then completing a subtasks sends it to the bottom so incomplete sub tasks
show up first
If screen size allows it have the big list of subtaks be displayed side by side
    For sub tasks touching the text or the checkbox should have the same effect

add timestamps of completion time can be an option

order for tasks completed show be most recently completed to the top

reucrring tasks

---

MAGIC 8 ball program 


---
Last changes
1. Loading States

  Added IsLoading property to prevent duplicate operations in:
  - HabitViewModel - Prevents concurrent habit loading/deletion
  - TaskViewModel - Prevents concurrent task loading/deletion
  - EditHabitViewModel - Prevents double-saving with IsSaving
  - EditTaskViewModel - Prevents double-saving with IsSaving
  - StatisticsViewModel - Prevents concurrent statistics loading

  2. Try-Catch Error Handling

  All data operations now wrapped in try-catch blocks with user-friendly error messages:

  HabitViewModel:
  - LoadHabitsAsync() - Catches and displays habit loading errors
  - OnDeleteHabitAsync() - Catches and displays deletion errors
  - OnToggleCompletion() - Catches and displays toggle errors

  EditHabitViewModel:
  - LoadHabitAsync() - Catches load errors and navigates back
  - OnSaveAsync() - Catches save errors with detailed messages

  TaskViewModel:
  - ApplyPriorityFilterAsync() - Catches and displays task loading errors
  - OnDeleteTaskAsync() - Catches and displays deletion errors
  - OnToggleTaskCompletionAsync() - Catches toggle errors
  - OnToggleSubTaskCompletionAsync() - Catches subtask toggle errors

  EditTaskViewModel:
  - LoadTaskAsync() - Catches load errors and navigates back
  - SaveTaskAsync() - Catches save errors with detailed messages

  StatisticsViewModel:
  - LoadStatisticsAsync() - Catches and displays statistics loading errors

  3. Validation

  Added input validation with clear user feedback:

  EditHabitViewModel:
  - Validates habit name is not empty
  - Validates at least one day is selected when not tracking everyday
  - Shows "Validation Error" alerts with specific messages

  EditTaskViewModel:
  - Validates task name is not empty
  - Shows "Validation Error" alert with specific message

  4. Confirmation Dialogs

  Added confirmation prompts for destructive actions:
  - Delete Habit - "Are you sure you want to delete this habit? This action cannot be undone."
  - Delete Task - "Are you sure you want to delete this task? This action cannot be undone."

  5. Error Messages

  All error messages follow a consistent pattern:
  - Title: Describes the issue type (e.g., "Error", "Validation Error")
  - Message: Specific details about what failed (e.g., "Failed to load habits: {exception message}")
  - Button: "OK" to dismiss

  6. Graceful Failure Handling

  When critical operations fail:
  - Load errors in edit pages automatically navigate back to prevent broken state
  - All finally blocks ensure loading states are cleared
  - UI remains responsive even when operations fail

---

 SQLite Persistence Implementation Guide for .NET MAUI

  Let me provide you with a comprehensive explanation of how we would implement SQLite persistence for your Tracker application. This
  will work seamlessly on both Windows and Android.

  Why SQLite Instead of JSON?

  JSON File Approach (what you've done before):
  - Simple: Just serialize/deserialize objects to/from a file
  - Downsides:
    - Must load entire file into memory
    - Must save entire file every time (no partial updates)
    - No querying capability (must load everything, then filter in C#)
    - No concurrent access control
    - No transactions (if app crashes mid-save, data corrupts)

  SQLite Approach:
  - Relational database stored in a single file
  - Can query specific data without loading everything
  - Supports transactions (all-or-nothing saves)
  - Indexes for fast lookups
  - Handles concurrent access
  - Industry standard (used by Android, iOS, browsers)
  - Still just a file - no server needed!

  ---
  How SQLite Works in .NET MAUI

  The Big Picture

  Your App (C#)
      ↓
  SQLite-net-pcl NuGet Package (C# wrapper)
      ↓
  SQLite Engine (native C library)
      ↓
  Database File (e.g., tracker.db3)

  The SQLite database is literally just a file on disk (like tracker.db3). The SQLite-net library:
  1. Opens the file
  2. Translates your C# code into SQL queries
  3. Executes them against the database
  4. Returns C# objects back to you

  Cross-Platform File Locations

  Windows:
  C:\Users\{username}\AppData\Local\Packages\{app-package-id}\LocalState\tracker.db3

  Android:
  /data/data/com.companyname.tracker/files/tracker.db3

  .NET MAUI provides a platform-agnostic API to get the correct path:
  string dbPath = Path.Combine(
      FileSystem.AppDataDirectory,  // MAUI figures out the right folder
      "tracker.db3"
  );

  ---
  Required NuGet Packages

  You'll need two packages:

  1. SQLite-net-pcl (v1.9.172 or newer)

  - The main SQLite library
  - Provides ORM (Object-Relational Mapping)
  - Lets you work with C# objects instead of writing raw SQL

  2. SQLiteNetExtensions (v2.1.0 or newer)

  - Adds relationship support (foreign keys, one-to-many, etc.)
  - Makes it easier to handle your Habit → HabitCompletion relationships
  - Handles cascading deletes automatically

  Installation command:
  dotnet add package sqlite-net-pcl
  dotnet add package SQLiteNetExtensions

  ---
  Database Design: Tables & Relationships

  Understanding ORM (Object-Relational Mapping)

  You currently have C# classes like:
  public class Habit
  {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public List<HabitCompletion> Completions { get; set; } // In-memory list
  }

  SQLite-net will map this to a table:
  Habits Table:
  +--------------------------------------+------------------+
  | Id (TEXT PRIMARY KEY)                | Name (TEXT)      |
  +--------------------------------------+------------------+
  | "abc-123..."                         | "Morning Run"    |
  +--------------------------------------+------------------+

  And HabitCompletion becomes another table:
  HabitCompletions Table:
  +--------------------------------------+--------------------------------------+
  | Id (TEXT PRIMARY KEY)                | HabitId (TEXT FOREIGN KEY)           |
  +--------------------------------------+--------------------------------------+
  | "completion-1"                       | "abc-123..."                         |
  +--------------------------------------+--------------------------------------+

  The HabitId column is a foreign key pointing back to the Habit table.

  Your Current Data Structure

  Let me analyze what we need to store:

  Habits:
  - Core properties: Id, Name, Description, TrackEveryday, NotesEnabled, etc.
  - Collection: List<HabitCompletion> (one-to-many relationship)
  - Collection: List<DayOfWeek> (needs special handling - we'll make a separate table)

  HabitCompletions:
  - Properties: Id, HabitId (foreign key), CompletedDate, Note

  Tasks:
  - Core properties: Id, Name, Description, DueDate, Priority, etc.
  - Collection: List<SubTask> (one-to-many relationship)

  SubTasks:
  - Properties: Id, ParentTaskId (foreign key), Name, IsCompleted

  Tables We'll Create

  1. Habits - Main habit data
  2. HabitCompletions - Completion records for habits
  3. HabitTrackingDays - Which days each habit tracks (Mon, Tue, etc.)
  4. TodoTasks - Main task data
  5. SubTasks - Subtasks belonging to tasks

  ---
  Step-by-Step Implementation Plan

  Phase 1: Add Attributes to Your Models

  You'll add attributes to your existing model classes to tell SQLite how to map them:

  using SQLite;
  using SQLiteNetExtensions.Attributes;

  public class Habit
  {
      [PrimaryKey]
      public Guid Id { get; set; }

      [MaxLength(200)]
      public string Name { get; set; }

      [MaxLength(1000)]
      public string Description { get; set; }

      public bool TrackEveryday { get; set; }

      // This tells SQLite NOT to store this property in the Habits table
      [Ignore]
      public List<DayOfWeek> TrackingDays { get; set; } = new();

      // This creates a relationship to HabitCompletion table
      [OneToMany(CascadeOperations = CascadeOperation.All)]
      public List<HabitCompletion> Completions { get; set; } = new();

      public DateTime CreatedDate { get; set; }
      public int DisplayOrder { get; set; }
      // ... other properties
  }

  Key Attributes:
  - [PrimaryKey] - Marks the unique identifier
  - [MaxLength(n)] - Sets max string length (good practice for performance)
  - [Ignore] - Tells SQLite "don't store this in the database"
  - [OneToMany] - Defines a parent-child relationship
  - [ForeignKey] - Points to a parent record

  Phase 2: Create a New Table for TrackingDays

  Since List<DayOfWeek> can't be stored directly, we create a junction table:

  [Table("HabitTrackingDays")]
  public class HabitTrackingDay
  {
      [PrimaryKey, AutoIncrement]
      public int Id { get; set; }

      [ForeignKey(typeof(Habit))]
      public Guid HabitId { get; set; }

      public DayOfWeek DayOfWeek { get; set; }
  }

  This lets one habit have multiple tracking days:
  HabitTrackingDays:
  +----+-------------+-------------+
  | Id | HabitId     | DayOfWeek   |
  +----+-------------+-------------+
  | 1  | "habit-123" | Monday      |
  | 2  | "habit-123" | Wednesday   |
  | 3  | "habit-123" | Friday      |
  +----+-------------+-------------+

  Phase 3: Create Database Service

  Create a new DatabaseService.cs that handles all SQLite operations:

  public class DatabaseService
  {
      private SQLiteAsyncConnection _database;

      public DatabaseService()
      {
          // Get platform-specific path
          string dbPath = Path.Combine(
              FileSystem.AppDataDirectory,
              "tracker.db3"
          );

          _database = new SQLiteAsyncConnection(dbPath);

          // Create tables if they don't exist
          _database.CreateTableAsync<Habit>().Wait();
          _database.CreateTableAsync<HabitCompletion>().Wait();
          _database.CreateTableAsync<HabitTrackingDay>().Wait();
          _database.CreateTableAsync<TodoTask>().Wait();
          _database.CreateTableAsync<SubTask>().Wait();
      }

      // Habit methods
      public Task<List<Habit>> GetAllHabitsAsync()
      {
          return _database.GetAllWithChildrenAsync<Habit>();
      }

      public Task<Habit> GetHabitByIdAsync(Guid id)
      {
          return _database.GetWithChildrenAsync<Habit>(id);
      }

      public async Task SaveHabitAsync(Habit habit)
      {
          // Insert or update with relationships
          await _database.InsertOrReplaceWithChildrenAsync(habit);

          // Handle TrackingDays separately
          await _database.ExecuteAsync(
              "DELETE FROM HabitTrackingDays WHERE HabitId = ?",
              habit.Id
          );

          foreach (var day in habit.TrackingDays)
          {
              await _database.InsertAsync(new HabitTrackingDay
              {
                  HabitId = habit.Id,
                  DayOfWeek = day
              });
          }
      }

      // Similar methods for Tasks, Completions, etc.
  }

  Key Methods:
  - CreateTableAsync<T>() - Creates table from C# class
  - GetAllWithChildrenAsync<T>() - Loads objects with their related data
  - InsertOrReplaceAsync() - Saves (insert if new, update if exists)
  - InsertOrReplaceWithChildrenAsync() - Saves with relationships
  - DeleteAsync() - Removes a record
  - ExecuteAsync() - Runs raw SQL when needed

  Phase 4: Update DataService to Use Database

  Modify your existing DataService.cs to use DatabaseService instead of in-memory lists:

  Before (in-memory):
  public class DataService : IDataService
  {
      private readonly List<Habit> _habits;

      public Task<List<Habit>> GetAllHabitsAsync()
      {
          var habits = _habits.OrderBy(h => h.DisplayOrder).ToList();
          return Task.FromResult(habits);
      }
  }

  After (SQLite):
  public class DataService : IDataService
  {
      private readonly DatabaseService _database;

      public DataService(DatabaseService database)
      {
          _database = database;
      }

      public async Task<List<Habit>> GetAllHabitsAsync()
      {
          var habits = await _database.GetAllHabitsAsync();

          // Load tracking days for each habit
          foreach (var habit in habits)
          {
              var trackingDays = await _database.GetTrackingDaysAsync(habit.Id);
              habit.TrackingDays = trackingDays.Select(td => td.DayOfWeek).ToList();
          }

          return habits.OrderBy(h => h.DisplayOrder).ToList();
      }
  }

  Phase 5: Register DatabaseService in Dependency Injection

  Update MauiProgram.cs:

  public static MauiApp CreateMauiApp()
  {
      var builder = MauiApp.CreateBuilder();
      builder
          .UseMauiApp<App>()
          .ConfigureFonts(...);

      // Register DatabaseService as singleton (one instance for app lifetime)
      builder.Services.AddSingleton<DatabaseService>();

      // DataService now depends on DatabaseService
      builder.Services.AddSingleton<IDataService, DataService>();

      // ViewModels, pages, etc.
      builder.Services.AddTransient<HabitViewModel>();
      // ...

      return builder.Build();
  }

  ---
  Important Considerations

  1. Async All The Way

  SQLite operations are I/O bound (reading/writing to disk), so they should always be async:

  // GOOD ✓
  await _database.InsertAsync(habit);

  // BAD ✗ - Blocks the UI thread
  _database.InsertAsync(habit).Wait();

  Good news: You already made everything async in step 2!

  2. Transactions for Data Integrity

  When saving complex objects, use transactions to ensure all-or-nothing:

  public async Task SaveTaskWithSubtasksAsync(TodoTask task)
  {
      await _database.RunInTransactionAsync((connection) =>
      {
          connection.InsertOrReplace(task);

          foreach (var subtask in task.SubTasks)
          {
              connection.InsertOrReplace(subtask);
          }
      });
      // If ANY operation fails, NOTHING is saved
  }

  3. Migrations (Database Schema Changes)

  When you update your models (add/remove properties), you need to handle migration:

  Option A: Simple (for development):
  // Drop and recreate tables (loses all data!)
  await _database.DropTableAsync<Habit>();
  await _database.CreateTableAsync<Habit>();

  Option B: Production-ready:
  public class DatabaseService
  {
      private const int CurrentDatabaseVersion = 2;

      public async Task InitializeAsync()
      {
          await _database.CreateTableAsync<Habit>();

          int version = GetDatabaseVersion();

          if (version < CurrentDatabaseVersion)
          {
              await MigrateDatabase(version);
          }
      }

      private async Task MigrateDatabase(int fromVersion)
      {
          if (fromVersion < 2)
          {
              // Add new column to existing table
              await _database.ExecuteAsync(
                  "ALTER TABLE Habits ADD COLUMN NewProperty TEXT"
              );
          }
      }
  }

  4. Loading Related Data

  Lazy Loading vs. Eager Loading:

  // Lazy: Load habit, then load completions separately (2 queries)
  var habit = await _database.GetAsync<Habit>(habitId);
  habit.Completions = await _database.Table<HabitCompletion>()
      .Where(c => c.HabitId == habitId)
      .ToListAsync();

  // Eager: Load everything in one go (better performance)
  var habit = await _database.GetWithChildrenAsync<Habit>(habitId, recursive: true);

  Use GetWithChildrenAsync when you know you'll need the related data.

  5. Indexing for Performance

  Add indexes to columns you search/filter frequently:

  public class HabitCompletion
  {
      [PrimaryKey]
      public Guid Id { get; set; }

      [ForeignKey(typeof(Habit)), Indexed]  // ← Index this!
      public Guid HabitId { get; set; }

      [Indexed]  // ← Index dates for range queries
      public DateTime CompletedDate { get; set; }
  }

  Indexes make queries like "find all completions for this habit" much faster.

  ---
  Data Migration Strategy

  Moving from In-Memory to SQLite

  Option 1: Start Fresh
  - Delete old in-memory code
  - Let users start with empty database
  - Simple but users lose data

  Option 2: One-Time Migration
  - Keep LoadSampleData() but modify it to check if database is empty
  - If empty, create sample data and save to SQLite
  - Users keep sample data

  public class DataService
  {
      private readonly DatabaseService _database;

      public DataService(DatabaseService database)
      {
          _database = database;
          _ = InitializeAsync();
      }

      private async Task InitializeAsync()
      {
          var existingHabits = await _database.GetAllHabitsAsync();

          if (existingHabits.Count == 0)
          {
              // First run - add sample data
              await LoadSampleDataAsync();
          }
      }

      private async Task LoadSampleDataAsync()
      {
          var exerciseHabit = new Habit
          {
              Id = Guid.NewGuid(),
              Name = "Morning Exercise",
              // ... sample data
          };

          await _database.SaveHabitAsync(exerciseHabit);
          // ... save other samples
      }
  }

  ---
  Testing the Implementation

  1. Test Database Location

  Add a debug method to print the database path:

  public void ShowDatabasePath()
  {
      string dbPath = Path.Combine(FileSystem.AppDataDirectory, "tracker.db3");
      System.Diagnostics.Debug.WriteLine($"Database location: {dbPath}");
  }

  Windows: Open the path in File Explorer and verify tracker.db3 exists

  Android: Use ADB to pull the file:
  adb pull /data/data/com.companyname.tracker/files/tracker.db3 .

  2. Inspect Database with Tools

  DB Browser for SQLite (free tool):
  - Download: https://sqlitebrowser.org/
  - Open tracker.db3
  - Browse tables, view data, run queries
  - Great for debugging!

  3. Verify Data Persistence

  Test sequence:
  1. Create a habit "Test Habit"
  2. Close app completely
  3. Reopen app
  4. Verify "Test Habit" still exists ✓

  ---
  Complete File Structure After Implementation

  Tracker/
  ├── Models/
  │   ├── Habit.cs                 [Add SQLite attributes]
  │   ├── HabitCompletion.cs       [Add SQLite attributes]
  │   ├── HabitTrackingDay.cs      [NEW - junction table]
  │   ├── TodoTask.cs              [Add SQLite attributes]
  │   └── SubTask.cs               [Add SQLite attributes]
  │
  ├── Services/
  │   ├── IDataService.cs          [No changes]
  │   ├── DataService.cs           [Modify to use DatabaseService]
  │   └── DatabaseService.cs       [NEW - SQLite operations]
  │
  ├── ViewModels/                  [No changes needed!]
  ├── Views/                       [No changes needed!]
  └── MauiProgram.cs              [Register DatabaseService]

  ---
  Common Pitfalls & Solutions

  Pitfall 1: Circular References

  Problem:
  public class Habit
  {
      public List<HabitCompletion> Completions { get; set; }
  }

  public class HabitCompletion
  {
      public Habit Habit { get; set; }  // ← Circular!
  }

  Solution: Use [Ignore] on one side:
  public class HabitCompletion
  {
      [Ignore]
      public Habit ParentHabit { get; set; }  // Don't store in DB
  }

  Pitfall 2: Forgetting to Load Children

  Problem:
  var habit = await _database.GetAsync<Habit>(id);
  // habit.Completions is empty!

  Solution: Use the extensions:
  var habit = await _database.GetWithChildrenAsync<Habit>(id);
  // habit.Completions is populated ✓

  Pitfall 3: Guid as String

  SQLite doesn't have a native Guid type, so it stores as TEXT:

  // This works automatically, but be aware:
  public Guid Id { get; set; }
  // Stored as: "3f2504e0-4f89-11d3-9a0c-0305e82c3301"

  Pitfall 4: DateTime Time Zones

  SQLite stores DateTime as UTC string. Your dates should be consistent:

  // GOOD ✓
  habit.CreatedDate = DateTime.UtcNow;

  // BAD ✗ - Mixing local and UTC causes bugs
  habit.CreatedDate = DateTime.Now;

  ---
  Performance Characteristics

  Read Performance:
  - Getting all habits: ~1-5ms (100 records)
  - Getting specific habit: ~0.1ms (indexed lookup)
  - Complex queries: 10-50ms

  Write Performance:
  - Insert/Update: ~1-5ms per record
  - Batch inserts (transaction): ~100 records/second

  Storage:
  - Empty database: ~20KB
  - 100 habits + 1000 completions: ~200KB
  - Very efficient!

  ---
  Advantages Over JSON
  ┌───────────────────┬─────────────────────────────────────┬───────────────────────────────────┐
  │      Aspect       │              JSON File              │              SQLite               │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Speed             │ Slow for large data (must load all) │ Fast (queries only what's needed) │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Memory            │ High (entire file in RAM)           │ Low (only active data)            │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Queries           │ Must load → filter in C#            │ SQL queries on disk               │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Partial Updates   │ Must rewrite entire file            │ Update single record              │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Relationships     │ Manual management                   │ Built-in foreign keys             │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Transactions      │ None                                │ ACID compliant                    │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Concurrent Access │ Prone to corruption                 │ Safe locking                      │
  ├───────────────────┼─────────────────────────────────────┼───────────────────────────────────┤
  │ Cross-Platform    │ Easy                                │ Easy (with MAUI)                  │
  └───────────────────┴─────────────────────────────────────┴───────────────────────────────────┘
  ---
  Summary of Changes You'll Make

  1. Install NuGet packages: sqlite-net-pcl, SQLiteNetExtensions
  2. Add attributes to models: [PrimaryKey], [ForeignKey], [OneToMany], etc.
  3. Create junction table: HabitTrackingDay for DayOfWeek list
  4. Create DatabaseService: Wrapper around SQLiteAsyncConnection
  5. Update DataService: Replace in-memory lists with database calls
  6. Register in DI: Add DatabaseService singleton in MauiProgram.cs
  7. Test: Verify data persists across app restarts

  No changes needed to:
  - ViewModels (they already use async!)
  - Views/XAML
  - Your UI logic
  - Navigation

  ---
  Next Steps (When Ready to Implement)

  1. I'll install the NuGet packages
  2. Create the DatabaseService
  3. Add attributes to your models
  4. Create the HabitTrackingDay model
  5. Update DataService to use DatabaseService
  6. Test on Windows
  7. Test on Android
  8. Verify data persists

---

# PROMPT TO IMPROVE FILES
Read this <FILE>
I now want you to review this file and look for improvements in performance or code syntax to make it more readable.
Follow this guidelines
Performance is paramount and the most important metric.
All code changes you make should maintain the same behavior as the original code.
The new code changes cannot cause new warnings to show up.
Before making code changes understand what the purpouse of the original file is doing, if needed read related files to get the full context
