using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    [QueryProperty(nameof(HabitIdString), "id")]
    public class EditHabitViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private Guid _habitId;

        public string HabitIdString
        {
            set
            {
                if (Guid.TryParse(value, out var habitId))
                {
                    _habitId = habitId;
                    LoadHabit();
                }
            }
        }

        public Guid HabitId
        {
            get => _habitId;
            set
            {
                _habitId = value;
                LoadHabit();
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _trackEveryday = true;
        public bool TrackEveryday
        {
            get => _trackEveryday;
            set => SetProperty(ref _trackEveryday, value);
        }

        private bool _hasDeadline;
        public bool HasDeadline
        {
            get => _hasDeadline;
            set => SetProperty(ref _hasDeadline, value);
        }

        private DateTime? _deadline;
        public DateTime? Deadline
        {
            get => _deadline;
            set => SetProperty(ref _deadline, value);
        }

        private bool _hasReminders;
        public bool HasReminders
        {
            get => _hasReminders;
            set => SetProperty(ref _hasReminders, value);
        }

        private TimeSpan? _reminderTime;
        public TimeSpan? ReminderTime
        {
            get => _reminderTime;
            set => SetProperty(ref _reminderTime, value);
        }

        private bool _notesEnabled;
        public bool NotesEnabled
        {
            get => _notesEnabled;
            set => SetProperty(ref _notesEnabled, value);
        }

        public ObservableCollection<DayOfWeekItem> DaysOfWeek { get; set; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditHabitViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Title = "Edit Habit";

            DaysOfWeek = new ObservableCollection<DayOfWeekItem>
            {
                new DayOfWeekItem { Day = DayOfWeek.Monday, Name = "Monday" },
                new DayOfWeekItem { Day = DayOfWeek.Tuesday, Name = "Tuesday" },
                new DayOfWeekItem { Day = DayOfWeek.Wednesday, Name = "Wednesday" },
                new DayOfWeekItem { Day = DayOfWeek.Thursday, Name = "Thursday" },
                new DayOfWeekItem { Day = DayOfWeek.Friday, Name = "Friday" },
                new DayOfWeekItem { Day = DayOfWeek.Saturday, Name = "Saturday" },
                new DayOfWeekItem { Day = DayOfWeek.Sunday, Name = "Sunday" }
            };

            SaveCommand = new Command(OnSave);
            CancelCommand = new Command(OnCancel);
        }

        private void LoadHabit()
        {
            if (_habitId == Guid.Empty) return;

            var habit = _dataService.GetHabitById(_habitId);
            if (habit != null)
            {
                Name = habit.Name;
                Description = habit.Description;
                TrackEveryday = habit.TrackEveryday;
                HasDeadline = habit.Deadline.HasValue;
                Deadline = habit.Deadline;
                HasReminders = habit.HasReminders;
                ReminderTime = habit.ReminderTime;
                NotesEnabled = habit.NotesEnabled;

                foreach (var item in DaysOfWeek)
                {
                    item.IsSelected = habit.TrackingDays.Contains(item.Day);
                }
            }
        }

        private async void OnSave()
        {
            var habit = _habitId == Guid.Empty ? new Habit() : _dataService.GetHabitById(_habitId);
            if (habit == null) return;

            habit.Name = Name;
            habit.Description = Description;
            habit.TrackEveryday = TrackEveryday;
            habit.Deadline = HasDeadline ? Deadline : null;
            habit.HasReminders = HasReminders;
            habit.ReminderTime = HasReminders ? ReminderTime : null;
            habit.NotesEnabled = NotesEnabled;

            habit.TrackingDays.Clear();
            foreach (var item in DaysOfWeek)
            {
                if (item.IsSelected)
                {
                    habit.TrackingDays.Add(item.Day);
                }
            }

            _dataService.SaveHabit(habit);
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public class DayOfWeekItem : BaseViewModel
    {
        public DayOfWeek Day { get; set; }
        public string Name { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
