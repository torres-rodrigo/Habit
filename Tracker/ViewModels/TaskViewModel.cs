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
        private bool _showOverlay;
        private bool _isLoading;

        public ObservableCollection<TodoTask> PendingTasks { get; set; }
        public ObservableCollection<TodoTask> CompletedTasks { get; set; }
        public ObservableCollection<PriorityOption> PriorityFilterOptions { get; set; }

        public PriorityOption SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    _ = ApplyPriorityFilterAsync();
                }
            }
        }

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => SetProperty(ref _showOverlay, value);
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

            AddTaskCommand = new Command(OnAddTask);
            EditTaskCommand = new Command<Guid>(OnEditTask);
            DeleteTaskCommand = new Command<Guid>(async (id) => await OnDeleteTaskAsync(id));
            ToggleTaskCompletionCommand = new Command<Guid>(async (id) => await OnToggleTaskCompletionAsync(id));
            ToggleSubTaskCompletionCommand = new Command<(Guid taskId, Guid subTaskId)>(async (args) => await OnToggleSubTaskCompletionAsync(args));
            CloseOverlayCommand = new Command(() => ShowOverlay = false);
            NavigateToHabitCommand = new Command(OnNavigateToHabit);
            NavigateToTaskCommand = new Command(OnNavigateToTask);

            _ = LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            await ApplyPriorityFilterAsync();
        }

        private async Task ApplyPriorityFilterAsync()
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
                await _dataService.ToggleTaskCompletionAsync(taskId);

                // Find the task in either collection and move it to the appropriate one
                var taskInPending = PendingTasks.FirstOrDefault(t => t.Id == taskId);
                var taskInCompleted = CompletedTasks.FirstOrDefault(t => t.Id == taskId);

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

                // Find and update only the affected task
                var task = PendingTasks.FirstOrDefault(t => t.Id == args.taskId)
                          ?? CompletedTasks.FirstOrDefault(t => t.Id == args.taskId);

                if (task != null)
                {
                    var updatedTask = await _dataService.GetTaskByIdAsync(args.taskId);
                    if (updatedTask != null)
                    {
                        // Update the subtask in the existing task object
                        var subTask = task.SubTasks.FirstOrDefault(st => st.Id == args.subTaskId);
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
