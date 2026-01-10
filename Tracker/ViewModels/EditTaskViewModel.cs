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
    private string _selectedPriority = "Normal";

    public string? TaskId
    {
        get => _taskId;
        set
        {
            _taskId = value;
            OnPropertyChanged();
            LoadTask();
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

    public ObservableCollection<string> Priorities { get; } = new() { "Low", "Normal", "High" };
    public ObservableCollection<SubtaskItem> Subtasks { get; } = new();

    public ICommand AddSubtaskCommand { get; }
    public ICommand RemoveSubtaskCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditTaskViewModel(IDataService dataService)
    {
        _dataService = dataService;
        Title = "New Task";

        AddSubtaskCommand = new Command(AddSubtask);
        RemoveSubtaskCommand = new Command<SubtaskItem>(RemoveSubtask);
        SaveCommand = new Command(async () => await SaveTask());
        CancelCommand = new Command(async () => await Cancel());
    }

    private void LoadTask()
    {
        if (string.IsNullOrEmpty(TaskId) || !Guid.TryParse(TaskId, out var taskGuid))
        {
            Title = "New Task";
            return;
        }

        var task = _dataService.GetTaskById(taskGuid);
        if (task != null)
        {
            Title = "Edit Task";
            TaskName = task.Name;
            Description = task.Description ?? string.Empty;
            HasDueDate = task.DueDate.HasValue;
            DueDate = task.DueDate ?? DateTime.Today.AddDays(7);
            SelectedPriority = task.Priority ?? "Normal";

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

    private async Task SaveTask()
    {
        if (string.IsNullOrWhiteSpace(TaskName))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Please enter a task name", "OK");
            return;
        }

        var taskId = string.IsNullOrEmpty(TaskId) || !Guid.TryParse(TaskId, out var parsedId) 
            ? Guid.NewGuid() 
            : parsedId;

        // Get existing task to preserve properties like DisplayOrder, CreatedDate, etc.
        var existingTask = string.IsNullOrEmpty(TaskId) ? null : _dataService.GetTaskById(taskId);

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
            Priority = SelectedPriority,
            CreatedDate = existingTask?.CreatedDate ?? DateTime.Now,
            IsCompleted = existingTask?.IsCompleted ?? false,
            CompletedDate = existingTask?.CompletedDate,
            DisplayOrder = existingTask?.DisplayOrder ?? 0,
            HasReminders = existingTask?.HasReminders ?? false,
            ReminderTime = existingTask?.ReminderTime,
            SubTasks = subtaskList
        };

        _dataService.SaveTask(task);

        await Shell.Current.GoToAsync("..");
    }

    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class SubtaskItem
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
