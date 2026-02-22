using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    public class TaskViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private PriorityOption _selectedPriorityFilter;
        private DateTypeOption _selectedDateTypeFilter;
        private DatePeriodOption _selectedDatePeriodFilter;
        private bool _showOverlay;
        private bool _showCustomDatePopup = false;
        private bool _isLoading;
        private bool _isDatePeriodFilterEnabled;
        private string _customDateDisplayText = string.Empty;
        private DateTime? _customStartDate;
        private DateTime? _customEndDate;

        public ObservableCollection<TodoTask> PendingTasks { get; set; }
        public ObservableCollection<TodoTask> CompletedTasks { get; set; }
        public ObservableCollection<PriorityOption> PriorityFilterOptions { get; set; }
        public ObservableCollection<DateTypeOption> DateTypeFilterOptions { get; set; }
        public ObservableCollection<DatePeriodOption> DatePeriodFilterOptions { get; set; }

        public PriorityOption SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        public DateTypeOption SelectedDateTypeFilter
        {
            get => _selectedDateTypeFilter;
            set
            {
                if (SetProperty(ref _selectedDateTypeFilter, value))
                {
                    // Enable/disable date period filter based on selection
                    IsDatePeriodFilterEnabled = value?.Name != "NA";

                    // Clear custom date display and custom date range only when N/A is selected
                    if (value?.Name == "NA")
                    {
                        CustomDateDisplayText = string.Empty;
                        _customStartDate = null;
                        _customEndDate = null;
                    }
                    else
                    {
                        // If switching from N/A to Created/Completed and Period is Custom but no date is set, show popup
                        if (SelectedDatePeriodFilter?.Name == "Custom" &&
                            string.IsNullOrEmpty(CustomDateDisplayText))
                        {
                            _ = ShowCustomDateModalAsync();
                        }
                    }

                    _ = ApplyFiltersAsync();
                }
            }
        }

        public bool IsDatePeriodFilterEnabled
        {
            get => _isDatePeriodFilterEnabled;
            set => SetProperty(ref _isDatePeriodFilterEnabled, value);
        }

        public DatePeriodOption SelectedDatePeriodFilter
        {
            get => _selectedDatePeriodFilter;
            set
            {
                if (SetProperty(ref _selectedDatePeriodFilter, value))
                {
                    // Show custom date modal if Custom is selected
                    if (value?.Name == "Custom")
                    {
                        _ = ShowCustomDateModalAsync();
                    }
                    else
                    {
                        // Clear custom date display and custom date range when switching to non-custom period
                        CustomDateDisplayText = string.Empty;
                        _customStartDate = null;
                        _customEndDate = null;
                        _ = ApplyFiltersAsync();
                    }
                }
            }
        }

        public string CustomDateDisplayText
        {
            get => _customDateDisplayText;
            set => SetProperty(ref _customDateDisplayText, value);
        }

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => SetProperty(ref _showOverlay, value);
        }

        public bool ShowCustomDatePopup
        {
            get => _showCustomDatePopup;
            set => SetProperty(ref _showCustomDatePopup, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleTaskCompletionCommand { get; }
        public ICommand ToggleSubTaskCompletionCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand NavigateToHabitCommand { get; }
        public ICommand NavigateToTaskCommand { get; }

        public TaskViewModel(IDataService dataService)
        {
            _dataService = dataService;
            PendingTasks = new ObservableCollection<TodoTask>();
            CompletedTasks = new ObservableCollection<TodoTask>();

            PriorityFilterOptions = new ObservableCollection<PriorityOption>
            {
                new() { Name = "ALL", DisplayName = "ALL" },
                new() { Name = "None", DisplayName = "None" },
                new() { Name = "Low", DisplayName = "● Low" },
                new() { Name = "Medium", DisplayName = "⬡ Medium" },
                new() { Name = "High", DisplayName = "▼ High" }
            };
            _selectedPriorityFilter = PriorityFilterOptions[0];

            DateTypeFilterOptions = new ObservableCollection<DateTypeOption>
            {
                new() { Name = "NA", DisplayName = "N/A" },
                new() { Name = "Created", DisplayName = "Created" },
                new() { Name = "Completed", DisplayName = "Completed" }
            };
            _selectedDateTypeFilter = DateTypeFilterOptions[0]; // Default to N/A
            _isDatePeriodFilterEnabled = false; // Disabled by default

            DatePeriodFilterOptions = new ObservableCollection<DatePeriodOption>
            {
                new() { Name = "AllTime", DisplayName = "All time" },
                new() { Name = "CurrentYear", DisplayName = "Current Year" },
                new() { Name = "CurrentMonth", DisplayName = "Current Month" },
                new() { Name = "CurrentWeek", DisplayName = "Current Week" },
                new() { Name = "Today", DisplayName = "Today" },
                new() { Name = "Custom", DisplayName = "Custom" }
            };
            _selectedDatePeriodFilter = DatePeriodFilterOptions[2]; // Default to Current Month

            AddTaskCommand = new Command(OnAddTask);
            EditTaskCommand = new Command<Guid>(OnEditTask);
            DeleteTaskCommand = new Command<Guid>(async (id) => await OnDeleteTaskAsync(id));
            ToggleTaskCompletionCommand = new Command<Guid>(async (id) => await OnToggleTaskCompletionAsync(id));
            ToggleSubTaskCompletionCommand = new Command<(Guid taskId, Guid subTaskId)>(async (args) => await OnToggleSubTaskCompletionAsync(args));
            CloseOverlayCommand = new Command(() => ShowOverlay = false);
            NavigateToHabitCommand = new Command(OnNavigateToHabit);
            NavigateToTaskCommand = new Command(OnNavigateToTask);

            // Subscribe to custom date selection messages
#pragma warning disable CS0618 // Type or member is obsolete
            MessagingCenter.Subscribe<CustomDateViewModel, CustomDateResult>(this, "CustomDateSelected", (sender, result) =>
            {
                SetCustomDateRange(result.StartDate, result.EndDate, result.DisplayText);
                ShowCustomDatePopup = false;
            });

            MessagingCenter.Subscribe<CustomDateViewModel>(this, "CustomDateCancelled", (sender) =>
            {
                // Revert to previous selection (Current Month)
                _selectedDatePeriodFilter = DatePeriodFilterOptions[2];
                OnPropertyChanged(nameof(SelectedDatePeriodFilter));
                ShowCustomDatePopup = false;
            });
#pragma warning restore CS0618 // Type or member is obsolete

            _ = LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                PendingTasks.Clear();
                CompletedTasks.Clear();

                var allTasks = await _dataService.GetAllTasksAsync();

                // Apply priority filter
                var filteredTasks = SelectedPriorityFilter?.Name switch
                {
                    "ALL" => allTasks,
                    "None" => allTasks.Where(t => string.IsNullOrEmpty(t.Priority) || t.Priority == "None"),
                    _ => allTasks.Where(t => t.Priority == SelectedPriorityFilter?.Name)
                };

                // Apply date filter
                filteredTasks = ApplyDateFilter(filteredTasks);

                // Separate into pending and completed
                foreach (var task in filteredTasks)
                {
                    if (task.IsCompleted)
                        CompletedTasks.Add(task);
                    else
                        PendingTasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load tasks: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowCustomDateModalAsync()
        {
            ShowCustomDatePopup = true;
            await Task.CompletedTask;
        }

        public void SetCustomDateRange(DateTime startDate, DateTime endDate, string displayText)
        {
            _customStartDate = startDate;
            _customEndDate = endDate;
            CustomDateDisplayText = displayText;
            _ = ApplyFiltersAsync();
        }

        private IEnumerable<TodoTask> ApplyDateFilter(IEnumerable<TodoTask> tasks)
        {
            // If N/A is selected, don't apply any date filtering
            if (SelectedDateTypeFilter?.Name == "NA")
                return tasks;

            // Handle "All time" based on date type
            if (SelectedDatePeriodFilter?.Name == "AllTime")
            {
                if (SelectedDateTypeFilter?.Name == "Completed")
                {
                    // Show only completed tasks
                    return tasks.Where(t => t.IsCompleted && t.CompletedDate.HasValue);
                }
                else // Created
                {
                    // Show all tasks (all tasks have a created date)
                    return tasks;
                }
            }

            var today = DateTime.Today;
            DateTime startDate;
            DateTime endDate = today.AddDays(1).AddTicks(-1); // End of today

            switch (SelectedDatePeriodFilter?.Name)
            {
                case "Today":
                    startDate = today;
                    break;
                case "CurrentWeek":
                    // Start of week (Sunday)
                    startDate = today.AddDays(-(int)today.DayOfWeek);
                    endDate = startDate.AddDays(7).AddTicks(-1); // End of Saturday
                    break;
                case "CurrentMonth":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.AddMonths(1).AddTicks(-1); // End of month
                    break;
                case "CurrentYear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = new DateTime(today.Year, 12, 31, 23, 59, 59);
                    break;
                case "Custom":
                    if (_customStartDate.HasValue && _customEndDate.HasValue)
                    {
                        startDate = _customStartDate.Value;
                        endDate = _customEndDate.Value;
                    }
                    else
                    {
                        return tasks; // No custom date set, show all
                    }
                    break;
                default:
                    return tasks; // No filter
            }

            // Filter by date type (Created or Completed)
            if (SelectedDateTypeFilter?.Name == "Completed")
            {
                return tasks.Where(t => t.IsCompleted &&
                                       t.CompletedDate.HasValue &&
                                       t.CompletedDate.Value >= startDate &&
                                       t.CompletedDate.Value <= endDate);
            }
            else // Created
            {
                return tasks.Where(t => t.CreatedDate >= startDate &&
                                       t.CreatedDate <= endDate);
            }
        }

        private void OnAddTask()
        {
            // Show overlay instead of navigating
            ShowOverlay = true;
        }

        private async void OnNavigateToHabit()
        {
            ShowOverlay = false;
            await Shell.Current.GoToAsync("//habits/edithabit");
        }

        private async void OnNavigateToTask()
        {
            ShowOverlay = false;
            await Shell.Current.GoToAsync("//tasks/edittask");
        }

        private async void OnEditTask(Guid taskId)
        {
            // Navigate to edit task page
            await Shell.Current.GoToAsync($"//tasks/edittask?id={taskId}");
        }

        private async Task OnDeleteTaskAsync(Guid taskId)
        {
            if (IsLoading) return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Delete Task",
                    "Are you sure you want to delete this task? This action cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!confirm) return;

                IsLoading = true;
                await _dataService.DeleteTaskAsync(taskId);
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete task: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnToggleTaskCompletionAsync(Guid taskId)
        {
            try
            {
                // Check if task is currently completed
                var taskInCompleted = CompletedTasks.FirstOrDefault(t => t.Id == taskId);

                if (taskInCompleted != null)
                {
                    // Task is being unmarked as complete - ask for confirmation
                    var confirm = await Shell.Current.DisplayAlert(
                        "Mark as Incomplete",
                        "Are you sure you want to mark this task as incomplete? This will change the completion date and may affect your statistics.",
                        "OK",
                        "Cancel");

                    if (!confirm) return; // User cancelled
                }

                await _dataService.ToggleTaskCompletionAsync(taskId);

                // Find the task in either collection and move it to the appropriate one
                var taskInPending = PendingTasks.FirstOrDefault(t => t.Id == taskId);
                taskInCompleted = CompletedTasks.FirstOrDefault(t => t.Id == taskId);

                if (taskInPending != null)
                {
                    // Task was pending, now completed - move to completed collection
                    var updatedTask = await _dataService.GetTaskByIdAsync(taskId);
                    if (updatedTask != null)
                    {
                        PendingTasks.Remove(taskInPending);
                        CompletedTasks.Add(updatedTask);
                    }
                }
                else if (taskInCompleted != null)
                {
                    // Task was completed, now pending - move to pending collection
                    var updatedTask = await _dataService.GetTaskByIdAsync(taskId);
                    if (updatedTask != null)
                    {
                        CompletedTasks.Remove(taskInCompleted);
                        PendingTasks.Add(updatedTask);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to toggle task completion: {ex.Message}", "OK");
            }
        }

        private async Task OnToggleSubTaskCompletionAsync((Guid taskId, Guid subTaskId) args)
        {
            try
            {
                await _dataService.ToggleSubTaskCompletionAsync(args.taskId, args.subTaskId);

                // Get the updated task to check if it was auto-completed
                var updatedTask = await _dataService.GetTaskByIdAsync(args.taskId);
                if (updatedTask == null) return;

                // Find the task in either collection
                var taskInPending = PendingTasks.FirstOrDefault(t => t.Id == args.taskId);
                var taskInCompleted = CompletedTasks.FirstOrDefault(t => t.Id == args.taskId);

                // Check if the parent task's completion status changed (auto-complete triggered)
                if (taskInPending != null && updatedTask.IsCompleted)
                {
                    // Task was auto-completed - move to completed collection
                    PendingTasks.Remove(taskInPending);
                    CompletedTasks.Add(updatedTask);
                }
                else if (taskInPending != null)
                {
                    // Task is still pending - just update the subtask
                    var subTask = taskInPending.SubTasks.FirstOrDefault(st => st.Id == args.subTaskId);
                    if (subTask != null)
                    {
                        var updatedSubTask = updatedTask.SubTasks.FirstOrDefault(st => st.Id == args.subTaskId);
                        if (updatedSubTask != null)
                        {
                            subTask.IsCompleted = updatedSubTask.IsCompleted;
                        }
                    }
                }
                else if (taskInCompleted != null)
                {
                    // Task is in completed - just update the subtask
                    var subTask = taskInCompleted.SubTasks.FirstOrDefault(st => st.Id == args.subTaskId);
                    if (subTask != null)
                    {
                        var updatedSubTask = updatedTask.SubTasks.FirstOrDefault(st => st.Id == args.subTaskId);
                        if (updatedSubTask != null)
                        {
                            subTask.IsCompleted = updatedSubTask.IsCompleted;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to toggle subtask completion: {ex.Message}", "OK");
            }
        }

        public async Task RefreshAsync()
        {
            await LoadTasksAsync();
        }
    }
}
