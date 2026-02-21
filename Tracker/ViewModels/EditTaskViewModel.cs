using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels;

[QueryProperty(nameof(TaskId), "id")]
public class EditTaskViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private string? _taskId;
    private string _taskName = string.Empty;
    private string _description = string.Empty;
    private bool _hasDueDate;
    private DateTime _dueDate = DateTime.Today.AddDays(7);
    private string _selectedPriority = "None";
    private bool _hasReminders;
    private TimeSpan _reminderTime = new TimeSpan(9, 0, 0);
    private bool _autoCompleteWithSubtasks;
    private bool _isSaving;

    public string? TaskId
    {
        get => _taskId;
        set
        {
            _taskId = value;
            OnPropertyChanged();
            _ = LoadTaskAsync();
        }
    }

    public string TaskName
    {
        get => _taskName;
        set => SetProperty(ref _taskName, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool HasDueDate
    {
        get => _hasDueDate;
        set => SetProperty(ref _hasDueDate, value);
    }

    public DateTime DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public string SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public bool HasReminders
    {
        get => _hasReminders;
        set => SetProperty(ref _hasReminders, value);
    }

    public TimeSpan ReminderTime
    {
        get => _reminderTime;
        set => SetProperty(ref _reminderTime, value);
    }

    public bool AutoCompleteWithSubtasks
    {
        get => _autoCompleteWithSubtasks;
        set => SetProperty(ref _autoCompleteWithSubtasks, value);
    }

    public ObservableCollection<string> Priorities { get; } = new() { "None", "● Low", "⬡ Medium", "▼ High" };
    public ObservableCollection<SubtaskItem> Subtasks { get; } = new();

    public ICommand AddSubtaskCommand { get; }
    public ICommand RemoveSubtaskCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public EditTaskViewModel(IDataService dataService)
    {
        _dataService = dataService;
        Title = "New Task";

        AddSubtaskCommand = new Command(AddSubtask);
        RemoveSubtaskCommand = new Command<SubtaskItem>(RemoveSubtask);
        SaveCommand = new Command(async () => await SaveTaskAsync());
        CancelCommand = new Command(async () => await Cancel());
    }

    private async Task LoadTaskAsync()
    {
        if (string.IsNullOrEmpty(TaskId) || !Guid.TryParse(TaskId, out var taskGuid))
        {
            Title = "New Task";
            return;
        }

        try
        {
            var task = await _dataService.GetTaskByIdAsync(taskGuid);
            if (task != null)
            {
                Title = "Edit Task";
                TaskName = task.Name;
                Description = task.Description ?? string.Empty;
                HasDueDate = task.DueDate.HasValue;
                DueDate = task.DueDate ?? DateTime.Today.AddDays(7);
                SelectedPriority = FormatPriorityForDisplay(task.Priority ?? "None");
                AutoCompleteWithSubtasks = task.AutoCompleteWithSubtasks;

                Subtasks.Clear();
                foreach (var subtask in task.SubTasks)
                {
                    Subtasks.Add(new SubtaskItem
                    {
                        Id = subtask.Id,
                        Name = subtask.Name,
                        IsCompleted = subtask.IsCompleted
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to load task: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private void AddSubtask()
    {
        Subtasks.Add(new SubtaskItem { Name = string.Empty });
    }

    private void RemoveSubtask(SubtaskItem? subtask)
    {
        if (subtask != null)
        {
            Subtasks.Remove(subtask);
        }
    }

    private async Task SaveTaskAsync()
    {
        if (IsSaving) return;

        // Validate input
        if (string.IsNullOrWhiteSpace(TaskName))
        {
            await Shell.Current.DisplayAlert("Validation Error", "Please enter a task name.", "OK");
            return;
        }

        try
        {
            IsSaving = true;
            var taskId = string.IsNullOrEmpty(TaskId) || !Guid.TryParse(TaskId, out var parsedId)
                ? Guid.NewGuid()
                : parsedId;

            // Get existing task to preserve properties like DisplayOrder, CreatedDate, etc.
            var existingTask = string.IsNullOrEmpty(TaskId) ? null : await _dataService.GetTaskByIdAsync(taskId);

            var subtaskList = Subtasks
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select((s, index) => new SubTask
                {
                    Id = s.Id ?? Guid.NewGuid(),
                    ParentTaskId = taskId,
                    Name = s.Name,
                    IsCompleted = s.IsCompleted,
                    DisplayOrder = index
                })
                .ToList();

            var task = new TodoTask
            {
                Id = taskId,
                Name = TaskName,
                Description = string.IsNullOrWhiteSpace(Description) ? string.Empty : Description,
                DueDate = HasDueDate ? DueDate : null,
                Priority = ExtractPriorityName(SelectedPriority),
                CreatedDate = existingTask?.CreatedDate ?? DateTime.Now,
                IsCompleted = existingTask?.IsCompleted ?? false,
                CompletedDate = existingTask?.CompletedDate,
                DisplayOrder = existingTask?.DisplayOrder ?? 0,
                HasReminders = existingTask?.HasReminders ?? false,
                ReminderTime = existingTask?.ReminderTime,
                AutoCompleteWithSubtasks = AutoCompleteWithSubtasks,
                SubTasks = subtaskList
            };

            await _dataService.SaveTaskAsync(task);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save task: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static string ExtractPriorityName(string displayPriority)
    {
        if (string.IsNullOrEmpty(displayPriority)) return "None";
        
        // Remove symbols: ●, ⬡, ▼ and trim
        return displayPriority.Replace("●", "").Replace("⬡", "").Replace("▼", "").Trim();
    }

    private static string FormatPriorityForDisplay(string priority)
    {
        return priority switch
        {
            "Low" => "● Low",
            "Medium" => "⬡ Medium",
            "High" => "▼ High",
            _ => "None"
        };
    }
}

public class SubtaskItem : BaseViewModel
{
    private Guid? _id;
    private string _name = string.Empty;
    private bool _isCompleted;

    public Guid? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }
}
