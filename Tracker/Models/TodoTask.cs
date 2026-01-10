using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Tracker.Models
{
    public class TodoTask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifySubTasksChanged()
        {
            OnPropertyChanged(nameof(CompletedSubTasksCount));
            OnPropertyChanged(nameof(TotalSubTasksCount));
            OnPropertyChanged(nameof(SubTaskCompletionPercentage));
            OnPropertyChanged(nameof(AllSubTasksCompleted));
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public string? Priority { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasReminders { get; set; }
        public TimeSpan? ReminderTime { get; set; }
        public int DisplayOrder { get; set; }
        public List<SubTask> SubTasks { get; set; } = new();

        public bool AllSubTasksCompleted => SubTasks.Count > 0 && SubTasks.All(st => st.IsCompleted);
        public bool CompletedOnTime => CompletedDate.HasValue && DueDate.HasValue && CompletedDate.Value <= DueDate.Value;
        public bool CompletedAfterDeadline => CompletedDate.HasValue && DueDate.HasValue && CompletedDate.Value > DueDate.Value;
        
        // Subtask completion tracking
        public int CompletedSubTasksCount => SubTasks.Count(st => st.IsCompleted);
        public int TotalSubTasksCount => SubTasks.Count;
        public int SubTaskCompletionPercentage => TotalSubTasksCount > 0 
            ? (int)Math.Round((double)CompletedSubTasksCount / TotalSubTasksCount * 100) 
            : 0;
    }

    public class SubTask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Parent task reference for notifying completion changes
        private TodoTask? _parentTask;
        public void SetParentTask(TodoTask? parent)
        {
            _parentTask = parent;
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParentTaskId { get; set; }
        public string Name { get; set; } = string.Empty;
        
        private bool _isCompleted;
        public bool IsCompleted 
        { 
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                    _parentTask?.NotifySubTasksChanged();
                }
            }
        }
        
        public int DisplayOrder { get; set; }
    }
}
