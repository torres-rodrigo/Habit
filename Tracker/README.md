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

Once it is in a repo ask ai to remove warnings, do performance improvements do code clean up.

Notifications/reminders

Show priority and dead line on task card
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
  Creation date
  Due date

  Clicking a completed task should ask the user if they are sure they want to mark it as incomplete
  Are you sure you want to edit a completed task

  Out of date comment for taks

Make the Plus button "hover"

Add toggle in task creation for auto complete task if all subtasks are completed
slightly reduce the size of the subtask checkbox
reducing spacing between sub task check box and text
reduce spacing between subtasks
put a limit to the lenght of the name of a subtask
Have a maximum of 5 or seven subtasks that display if more scorll
When there is enough subtasks to scroll then completing a subtasks sends it to the bottom so incomplete sub tasks
show up first
display the number of sub tasks completed x/y and the percentage
If screen size allows it have the big list of subtaks be displayed side by side
For sub tasks touching the text or the checkbox should have the same effect

add timestamps of completion time can be an option