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
        private bool _isSaving;

        public string HabitIdString
        {
            set
            {
                if (Guid.TryParse(value, out var habitId))
                {
                    _habitId = habitId;
                    _ = LoadHabitAsync();
                }
            }
        }

        public Guid HabitId
        {
            get => _habitId;
            set
            {
                _habitId = value;
                _ = LoadHabitAsync();
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

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public EditHabitViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Title = "Edit Habit";

            DaysOfWeek = new ObservableCollection<DayOfWeekItem>
            {
                new() { Day = DayOfWeek.Monday, Name = "Monday" },
                new() { Day = DayOfWeek.Tuesday, Name = "Tuesday" },
                new() { Day = DayOfWeek.Wednesday, Name = "Wednesday" },
                new() { Day = DayOfWeek.Thursday, Name = "Thursday" },
                new() { Day = DayOfWeek.Friday, Name = "Friday" },
                new() { Day = DayOfWeek.Saturday, Name = "Saturday" },
                new() { Day = DayOfWeek.Sunday, Name = "Sunday" }
            };

            SaveCommand = new Command(async () => await OnSaveAsync());
            CancelCommand = new Command(OnCancel);
        }

        private async Task LoadHabitAsync()
        {
            if (_habitId == Guid.Empty) return;

            try
            {
                var habit = await _dataService.GetHabitByIdAsync(_habitId);
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
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load habit: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }

        private async Task OnSaveAsync()
        {
            if (IsSaving) return;

            // Validate input
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter a habit name.", "OK");
                return;
            }

            if (!TrackEveryday && !DaysOfWeek.Any(d => d.IsSelected))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select at least one day to track this habit.", "OK");
                return;
            }

            try
            {
                IsSaving = true;
                var habit = _habitId == Guid.Empty ? new Habit() : await _dataService.GetHabitByIdAsync(_habitId) ?? new Habit();

                habit.Name = Name;
                habit.Description = Description;
                habit.TrackEveryday = TrackEveryday;
                habit.Deadline = HasDeadline ? Deadline : null;
                habit.HasReminders = HasReminders;
                habit.ReminderTime = HasReminders ? ReminderTime : null;
                habit.NotesEnabled = NotesEnabled;

                habit.TrackingDays.Clear();
                foreach (var item in DaysOfWeek.Where(d => d.IsSelected))
                {
                    habit.TrackingDays.Add(item.Day);
                }

                await _dataService.SaveHabitAsync(habit);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to save habit: {ex.Message}", "OK");
            }
            finally
            {
                IsSaving = false;
            }
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
