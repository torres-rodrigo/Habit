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
        private readonly INotificationService _notificationService;
        private Guid _habitId;
        private bool _isSaving;
        private DateTime _currentDisplayMonth;
        private Habit? _loadedHabit;
        private bool _isMonthSelectionMode;
        private bool _isYearSelectionMode;
        private int _displayYear;
        private int _yearRangeStart;
        private DateTime _habitCreatedDate;
        private HashSet<DateTime> _originalCompletions = new();
        private HashSet<DateTime> _currentCompletions = new();
        private DateTime _selectedNoteDate = DateTime.Today;
        private string _noteText = string.Empty;

        public string HabitIdString
        {
            set
            {
                if (Guid.TryParse(value, out var habitId))
                {
                    _habitId = habitId;
                    OnPropertyChanged(nameof(IsEditingExistingHabit));
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
                OnPropertyChanged(nameof(IsEditingExistingHabit));
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
            set
            {
                if (SetProperty(ref _trackEveryday, value))
                {
                    BuildCalendarDays();
                }
            }
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
            set
            {
                if (SetProperty(ref _notesEnabled, value))
                {
                    OnPropertyChanged(nameof(ShowNotesSection));
                }
            }
        }

        private bool _isNegativeHabit;
        public bool IsNegativeHabit
        {
            get => _isNegativeHabit;
            set => SetProperty(ref _isNegativeHabit, value);
        }

        private bool _isTracked = true;
        public bool IsTracked
        {
            get => _isTracked;
            set
            {
                if (SetProperty(ref _isTracked, value))
                {
                    OnPropertyChanged(nameof(UntrackTrackButtonText));
                    OnPropertyChanged(nameof(UntrackTrackButtonColor));
                }
            }
        }

        public ObservableCollection<DayOfWeekItem> DaysOfWeek { get; set; }
        public ObservableCollection<CalendarDayViewModel> CalendarDays { get; set; }

        public DateTime CurrentDisplayMonth
        {
            get => _currentDisplayMonth;
            set
            {
                if (SetProperty(ref _currentDisplayMonth, value))
                {
                    OnPropertyChanged(nameof(CurrentMonthYearDisplay));
                    BuildCalendarDays();
                }
            }
        }

        public string CurrentMonthYearDisplay => CurrentDisplayMonth.ToString("MMMM yyyy");
        public string HabitColor => IsNegativeHabit ? "Red" : "Green";

        public ObservableCollection<CalendarWeekViewModel> CalendarWeeks { get; set; }
        public ObservableCollection<MonthItemViewModel> MonthItems { get; set; }
        public ObservableCollection<YearItemViewModel> YearItems { get; set; }

        public int YearRangeStart
        {
            get => _yearRangeStart;
            set
            {
                if (SetProperty(ref _yearRangeStart, value))
                {
                    UpdateYearItems();
                }
            }
        }

        public bool IsMonthSelectionMode
        {
            get => _isMonthSelectionMode;
            set
            {
                if (SetProperty(ref _isMonthSelectionMode, value))
                {
                    OnPropertyChanged(nameof(IsDayViewMode));
                    OnPropertyChanged(nameof(IsMonthSelectionOnly));
                }
            }
        }

        public bool IsYearSelectionMode
        {
            get => _isYearSelectionMode;
            set
            {
                if (SetProperty(ref _isYearSelectionMode, value))
                {
                    OnPropertyChanged(nameof(IsDayViewMode));
                    OnPropertyChanged(nameof(IsMonthSelectionOnly));
                }
            }
        }

        public bool IsDayViewMode => !IsMonthSelectionMode && !IsYearSelectionMode;
        public bool IsMonthSelectionOnly => IsMonthSelectionMode && !IsYearSelectionMode;

        public int DisplayYear
        {
            get => _displayYear;
            set
            {
                if (SetProperty(ref _displayYear, value))
                {
                    UpdateMonthItemsSelection();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand UntrackTrackCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand ToggleCalendarDayCommand { get; }
        public ICommand ToggleMonthSelectionCommand { get; }
        public ICommand SelectMonthCommand { get; }
        public ICommand PreviousYearCommand { get; }
        public ICommand NextYearCommand { get; }
        public ICommand ToggleYearSelectionCommand { get; }
        public ICommand SelectYearCommand { get; }
        public ICommand PreviousYearRangeCommand { get; }
        public ICommand NextYearRangeCommand { get; }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public bool IsEditingExistingHabit => _habitId != Guid.Empty;
        public string CreatedDateFormatted => _habitCreatedDate.ToString("dd/MM/yyyy");
        public string UntrackTrackButtonText => IsTracked ? "Untrack" : "Track";
        public string UntrackTrackButtonColor => IsTracked ? "#FFC107" : "Green";

        // Notes properties
        public DateTime SelectedNoteDate
        {
            get => _selectedNoteDate;
            set
            {
                if (SetProperty(ref _selectedNoteDate, value))
                {
                    OnPropertyChanged(nameof(SelectedNoteDateDisplay));
                    OnPropertyChanged(nameof(IsNoteDateTrackable));
                    OnPropertyChanged(nameof(SaveNoteButtonColor));
                    OnPropertyChanged(nameof(NoteEditorText));
                    _ = LoadNoteForDateAsync();
                }
            }
        }

        public string SelectedNoteDateDisplay => SelectedNoteDate.ToString("dd/MM/yyyy");

        public string NoteText
        {
            get => _noteText;
            set
            {
                if (SetProperty(ref _noteText, value))
                {
                    OnPropertyChanged(nameof(NoteEditorText));
                }
            }
        }

        /// <summary>
        /// Text to display in the note editor - shows "INVALID DATE" when date is not trackable
        /// </summary>
        public string NoteEditorText
        {
            get => IsNoteDateTrackable ? _noteText : "INVALID DATE";
            set
            {
                if (IsNoteDateTrackable)
                {
                    NoteText = value;
                }
            }
        }

        /// <summary>
        /// True if the selected note date is a day that should be tracked by the habit
        /// and is within the valid date range (creation date to today)
        /// </summary>
        public bool IsNoteDateTrackable
        {
            get
            {
                if (!IsEditingExistingHabit) return false;
                // Check if date is within valid range (creation date to today)
                if (SelectedNoteDate.Date < _habitCreatedDate.Date || SelectedNoteDate.Date > DateTime.Today)
                    return false;
                return ShouldTrackOnDate(SelectedNoteDate);
            }
        }

        public string SaveNoteButtonColor => IsNoteDateTrackable ? "#512BD4" : "#9E9E9E";

        public bool ShowNotesSection => NotesEnabled && IsEditingExistingHabit;

        public ICommand SaveNoteCommand { get; }

        public EditHabitViewModel(IDataService dataService, INotificationService notificationService)
        {
            _dataService = dataService;
            _notificationService = notificationService;
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

            // Wire up callback for day selection changes to rebuild calendar
            foreach (var day in DaysOfWeek)
            {
                day.SetOnSelectionChanged(() => BuildCalendarDays());
            }

            CalendarDays = new ObservableCollection<CalendarDayViewModel>();
            CalendarWeeks = new ObservableCollection<CalendarWeekViewModel>();
            _currentDisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _displayYear = DateTime.Today.Year;

            // Pre-create 6 weeks with 7 days each to avoid collection changes that cause scroll jumps
            for (int week = 0; week < 6; week++)
            {
                var calendarWeek = new CalendarWeekViewModel();
                for (int day = 0; day < 7; day++)
                {
                    var dayVm = new CalendarDayViewModel();
                    calendarWeek.Days.Add(dayVm);
                    CalendarDays.Add(dayVm);
                }
                CalendarWeeks.Add(calendarWeek);
            }

            // Initialize month items
            MonthItems = new ObservableCollection<MonthItemViewModel>();
            for (int i = 1; i <= 12; i++)
            {
                MonthItems.Add(new MonthItemViewModel
                {
                    MonthNumber = i,
                    MonthName = new DateTime(2000, i, 1).ToString("MMMM"),
                    IsSelected = i == DateTime.Today.Month
                });
            }

            // Initialize year items (show 12 years at a time, 4x3 grid)
            YearItems = new ObservableCollection<YearItemViewModel>();
            _yearRangeStart = DateTime.Today.Year - 5; // Center current year in the range
            for (int i = 0; i < 12; i++)
            {
                YearItems.Add(new YearItemViewModel
                {
                    Year = _yearRangeStart + i,
                    IsSelected = (_yearRangeStart + i) == DateTime.Today.Year
                });
            }

            SaveCommand = new Command(async () => await OnSaveAsync());
            CancelCommand = new Command(OnCancel);
            UntrackTrackCommand = new Command(async () => await OnUntrackTrackAsync());
            DeleteCommand = new Command(async () => await OnDeleteAsync());
            PreviousMonthCommand = new Command(OnPreviousMonth);
            NextMonthCommand = new Command(OnNextMonth);
            ToggleCalendarDayCommand = new Command<CalendarDayViewModel>(OnToggleCalendarDay);
            ToggleMonthSelectionCommand = new Command(OnToggleMonthSelection);
            SelectMonthCommand = new Command<MonthItemViewModel>(OnSelectMonth);
            PreviousYearCommand = new Command(() => DisplayYear--);
            NextYearCommand = new Command(() => DisplayYear++);
            ToggleYearSelectionCommand = new Command(OnToggleYearSelection);
            SelectYearCommand = new Command<YearItemViewModel>(OnSelectYear);
            PreviousYearRangeCommand = new Command(() => YearRangeStart -= 12);
            NextYearRangeCommand = new Command(() => YearRangeStart += 12);
            SaveNoteCommand = new Command(async () => await OnSaveNoteAsync());
        }

        private async Task LoadHabitAsync()
        {
            if (_habitId == Guid.Empty) return;

            try
            {
                var habit = await _dataService.GetHabitByIdAsync(_habitId);
                if (habit != null)
                {
                    _loadedHabit = habit;
                    _habitCreatedDate = habit.CreatedDate;

                    // Store original completions for comparison on save
                    _originalCompletions = habit.Completions.Select(c => c.CompletedDate.Date).ToHashSet();
                    _currentCompletions = new HashSet<DateTime>(_originalCompletions);

                    Name = habit.Name;
                    Description = habit.Description;
                    TrackEveryday = habit.TrackEveryday;
                    HasDeadline = habit.Deadline.HasValue;
                    Deadline = habit.Deadline;
                    HasReminders = habit.HasReminders;
                    ReminderTime = habit.ReminderTime;
                    NotesEnabled = habit.NotesEnabled;
                    IsNegativeHabit = habit.IsNegativeHabit;
                    IsTracked = habit.IsTracked;

                    foreach (var item in DaysOfWeek)
                    {
                        item.IsSelected = habit.TrackingDays.Contains(item.Day);
                    }

                    OnPropertyChanged(nameof(HabitColor));
                    OnPropertyChanged(nameof(ShowNotesSection));
                    OnPropertyChanged(nameof(IsNoteDateTrackable));
                    OnPropertyChanged(nameof(CreatedDateFormatted));
                    BuildCalendarDays();

                    // Load note for today's date
                    await LoadNoteForDateAsync();
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
                habit.IsNegativeHabit = IsNegativeHabit;

                // Track when habit was untracked
                bool wasTracked = habit.IsTracked;
                habit.IsTracked = IsTracked;

                if (wasTracked && !IsTracked)
                {
                    // Newly untracked - set the date
                    habit.UntrackedDate = DateTime.Now;
                }

                habit.TrackingDays.Clear();
                foreach (var item in DaysOfWeek.Where(d => d.IsSelected))
                {
                    habit.TrackingDays.Add(item.Day);
                }

                await _dataService.SaveHabitAsync(habit);

                // Save completion changes (only for existing habits)
                if (_habitId != Guid.Empty)
                {
                    // Find completions to add (in current but not in original)
                    var completionsToAdd = _currentCompletions.Except(_originalCompletions);
                    foreach (var date in completionsToAdd)
                    {
                        await _dataService.ToggleHabitCompletionAsync(_habitId, date);
                    }

                    // Find completions to remove (in original but not in current)
                    var completionsToRemove = _originalCompletions.Except(_currentCompletions);
                    foreach (var date in completionsToRemove)
                    {
                        await _dataService.ToggleHabitCompletionAsync(_habitId, date);
                    }
                }

                // Handle notifications
                if (HasReminders && ReminderTime.HasValue)
                {
                    // Request permissions first (Android only, no-op on Windows)
                    var permissionGranted = await _notificationService.RequestNotificationPermissionsAsync();
                    if (permissionGranted)
                    {
                        var trackingDays = TrackEveryday
                            ? new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                            : DaysOfWeek.Where(d => d.IsSelected).Select(d => d.Day).ToArray();

                        await _notificationService.ScheduleHabitReminderAsync(
                            habit.Id,
                            habit.Name,
                            habit.Description,
                            ReminderTime.Value,
                            trackingDays);
                    }
                }
                else
                {
                    // Cancel notifications if reminders are disabled
                    await _notificationService.CancelHabitReminderAsync(habit.Id);
                }

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

        private async Task OnUntrackTrackAsync()
        {
            try
            {
                // Only validate when editing an existing habit
                if (_habitId != Guid.Empty)
                {
                    var habit = await _dataService.GetHabitByIdAsync(_habitId);
                    if (habit == null) return;

                    // Check if habit is completed (past deadline)
                    bool isCompleted = habit.Deadline.HasValue && habit.Deadline.Value.Date < DateTime.Now.Date;

                    // Prevent untracking completed habits
                    if (isCompleted && IsTracked)
                    {
                        await Shell.Current.DisplayAlert(
                            "Cannot Untrack Completed Habit",
                            "Completed habits cannot be untracked. Please change the deadline to make it active first.",
                            "OK");
                        return;
                    }
                }

                // Toggle the tracked state (will be saved when user clicks Save)
                IsTracked = !IsTracked;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to toggle track status: {ex.Message}", "OK");
            }
        }

        private async Task OnDeleteAsync()
        {
            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Delete Habit",
                    "Are you sure you want to permanently delete this habit? This action cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!confirm) return;

                // Cancel any scheduled notifications for this habit
                await _notificationService.CancelHabitReminderAsync(_habitId);

                await _dataService.DeleteHabitAsync(_habitId);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete habit: {ex.Message}", "OK");
            }
        }

        private void OnPreviousMonth()
        {
            CurrentDisplayMonth = CurrentDisplayMonth.AddMonths(-1);
        }

        private void OnNextMonth()
        {
            CurrentDisplayMonth = CurrentDisplayMonth.AddMonths(1);
        }

        private void BuildCalendarDays()
        {
            if (!IsEditingExistingHabit || _loadedHabit == null) return;

            var firstOfMonth = new DateTime(CurrentDisplayMonth.Year, CurrentDisplayMonth.Month, 1);
            var startingDayOfWeek = (int)firstOfMonth.DayOfWeek;
            var startDate = firstOfMonth.AddDays(-startingDayOfWeek);

            // Update existing week/day objects in place (no collection changes = no scroll jump)
            for (int week = 0; week < 6; week++)
            {
                var weekStartDate = startDate.AddDays(week * 7);
                var weekNumber = GetIso8601WeekNumber(weekStartDate);

                var calendarWeek = CalendarWeeks[week];
                calendarWeek.WeekNumber = weekNumber;

                for (int day = 0; day < 7; day++)
                {
                    var date = weekStartDate.AddDays(day);
                    var isCurrentMonth = date.Month == CurrentDisplayMonth.Month;
                    var shouldTrack = ShouldTrackOnDate(date);
                    // Use _currentCompletions to show pending (unsaved) state
                    var isCompleted = _currentCompletions.Contains(date.Date);
                    // Only allow marking days from habit creation to today (inclusive)
                    var isWithinValidRange = date.Date >= _habitCreatedDate.Date && date.Date <= DateTime.Today;

                    var dayVm = calendarWeek.Days[day];
                    dayVm.Date = date;
                    dayVm.IsCurrentMonth = isCurrentMonth;
                    dayVm.IsCompleted = isCompleted;
                    dayVm.ShouldTrack = shouldTrack;
                    dayVm.IsNegativeHabit = IsNegativeHabit;
                    dayVm.IsWithinValidRange = isWithinValidRange;
                }
            }

            // Update month items selection when display month changes
            _displayYear = CurrentDisplayMonth.Year;
            OnPropertyChanged(nameof(DisplayYear));
            UpdateMonthItemsSelection();
        }

        private static int GetIso8601WeekNumber(DateTime date)
        {
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            var dayOfWeek = cal.GetDayOfWeek(date);
            if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }
            return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private void OnToggleMonthSelection()
        {
            if (IsMonthSelectionMode)
            {
                // Going back to day view - don't change anything
                IsMonthSelectionMode = false;
            }
            else
            {
                // Going to month selection - sync display year
                DisplayYear = CurrentDisplayMonth.Year;
                UpdateMonthItemsSelection();
                IsMonthSelectionMode = true;
            }
        }

        private void OnSelectMonth(MonthItemViewModel? month)
        {
            if (month == null) return;

            CurrentDisplayMonth = new DateTime(DisplayYear, month.MonthNumber, 1);
            IsMonthSelectionMode = false;
        }

        private void UpdateMonthItemsSelection()
        {
            foreach (var month in MonthItems)
            {
                month.IsSelected = month.MonthNumber == CurrentDisplayMonth.Month && DisplayYear == CurrentDisplayMonth.Year;
            }
        }

        private void OnToggleYearSelection()
        {
            if (IsYearSelectionMode)
            {
                // Going back to month selection
                IsYearSelectionMode = false;
            }
            else
            {
                // Going to year selection - center current display year in the range
                YearRangeStart = DisplayYear - 5;
                UpdateYearItems();
                IsYearSelectionMode = true;
            }
        }

        private void OnSelectYear(YearItemViewModel? yearItem)
        {
            if (yearItem == null) return;

            DisplayYear = yearItem.Year;
            UpdateMonthItemsSelection();
            IsYearSelectionMode = false;
        }

        private void UpdateYearItems()
        {
            for (int i = 0; i < YearItems.Count; i++)
            {
                var yearItem = YearItems[i];
                yearItem.Year = YearRangeStart + i;
                yearItem.IsSelected = yearItem.Year == DisplayYear;
            }
        }

        private bool ShouldTrackOnDate(DateTime date)
        {
            if (TrackEveryday) return true;
            return DaysOfWeek.Any(d => d.IsSelected && d.Day == date.DayOfWeek);
        }

        private void OnToggleCalendarDay(CalendarDayViewModel? day)
        {
            if (day == null || _habitId == Guid.Empty) return;

            // If clicking on a day from another month, navigate to that month
            if (!day.IsCurrentMonth)
            {
                CurrentDisplayMonth = new DateTime(day.Date.Year, day.Date.Month, 1);
                return;
            }

            // Update the selected note date to match the clicked day
            SelectedNoteDate = day.Date;

            // Only toggle completion for days that can be toggled
            // (trackable, within valid date range, and current month)
            if (!day.CanToggle) return;

            // Toggle the completion state locally (will be saved when user clicks Save)
            day.IsCompleted = !day.IsCompleted;
            var dateKey = day.Date.Date;

            if (day.IsCompleted)
            {
                _currentCompletions.Add(dateKey);
            }
            else
            {
                _currentCompletions.Remove(dateKey);
            }
        }

        private async Task LoadNoteForDateAsync()
        {
            if (_habitId == Guid.Empty) return;

            try
            {
                var noteText = await _dataService.GetHabitNoteAsync(_habitId, SelectedNoteDate);
                NoteText = noteText ?? string.Empty;
            }
            catch (Exception)
            {
                NoteText = string.Empty;
            }
        }

        private async Task OnSaveNoteAsync()
        {
            if (_habitId == Guid.Empty || !IsNoteDateTrackable) return;

            try
            {
                await _dataService.SaveHabitNoteAsync(_habitId, SelectedNoteDate, NoteText);
                await Shell.Current.DisplayAlert("Success", "Note saved successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to save note: {ex.Message}", "OK");
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
        private Action? _onSelectionChanged;

        public void SetOnSelectionChanged(Action callback)
        {
            _onSelectionChanged = callback;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    _onSelectionChanged?.Invoke();
                }
            }
        }
    }

    public class CalendarDayViewModel : BaseViewModel
    {
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set
            {
                if (SetProperty(ref _date, value))
                {
                    OnPropertyChanged(nameof(DayNumber));
                    OnPropertyChanged(nameof(IsToday));
                    OnPropertyChanged(nameof(BorderColor));
                    OnPropertyChanged(nameof(BorderThickness));
                }
            }
        }

        public int DayNumber => Date.Day;
        public bool IsToday => Date.Date == DateTime.Today;

        private bool _isCurrentMonth;
        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                if (SetProperty(ref _isCurrentMonth, value))
                {
                    OnPropertyChanged(nameof(Opacity));
                    OnPropertyChanged(nameof(FontWeight));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (SetProperty(ref _isCompleted, value))
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        private bool _shouldTrack;
        public bool ShouldTrack
        {
            get => _shouldTrack;
            set
            {
                if (SetProperty(ref _shouldTrack, value))
                {
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        private bool _isNegativeHabit;
        public bool IsNegativeHabit
        {
            get => _isNegativeHabit;
            set
            {
                if (SetProperty(ref _isNegativeHabit, value))
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(BorderColor));
                }
            }
        }

        private bool _isWithinValidRange;
        /// <summary>
        /// True if the date is between habit creation date and today (inclusive)
        /// </summary>
        public bool IsWithinValidRange
        {
            get => _isWithinValidRange;
            set
            {
                if (SetProperty(ref _isWithinValidRange, value))
                {
                    OnPropertyChanged(nameof(CanToggle));
                    OnPropertyChanged(nameof(Opacity));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        /// <summary>
        /// True if this day can be toggled (is trackable AND within valid date range)
        /// </summary>
        public bool CanToggle => ShouldTrack && IsWithinValidRange && IsCurrentMonth;

        // Visual properties for dark theme
        // Background only fills for completed days (not just today)
        // Use same green as habit cards ("Green" = #008000)
        public string BackgroundColor => IsCompleted ? (IsNegativeHabit ? "Red" : "Green") : "Transparent";

        // Border for today indicator (darker green outline)
        public string BorderColor => IsToday ? (IsNegativeHabit ? "#AA0000" : "#507d2a") : "Transparent";
        public double BorderThickness => IsToday ? 3.0 : 0.0;

        // Opacity: lower for non-current month days or days outside valid range
        public double Opacity
        {
            get
            {
                if (!IsCurrentMonth) return 0.35;
                if (!IsWithinValidRange) return 0.4;
                return 1.0;
            }
        }

        // Font styling - bold for current month
        public string FontWeight => IsCurrentMonth ? "Bold" : "None";

        public string TextColor
        {
            get
            {
                if (IsCompleted) return "#000000"; // Black text on filled circle
                if (!IsCurrentMonth) return "#666666"; // Gray for other months
                if (!ShouldTrack) return "#888888"; // Lighter gray for non-trackable
                return "#FFFFFF"; // White for trackable current month days
            }
        }
    }

    public class CalendarWeekViewModel : BaseViewModel
    {
        private int _weekNumber;
        public int WeekNumber
        {
            get => _weekNumber;
            set
            {
                if (SetProperty(ref _weekNumber, value))
                {
                    OnPropertyChanged(nameof(WeekLabel));
                }
            }
        }
        public string WeekLabel => $"W{WeekNumber}";
        public ObservableCollection<CalendarDayViewModel> Days { get; set; } = new();
    }

    public class MonthItemViewModel : BaseViewModel
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string ShortName => MonthName.Length > 3 ? MonthName[..3] : MonthName;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        public string BackgroundColor => IsSelected ? "Green" : "Transparent";
        public string TextColor => IsSelected ? "White" : "#FFFFFF";
    }

    public class YearItemViewModel : BaseViewModel
    {
        private int _year;
        public int Year
        {
            get => _year;
            set
            {
                if (SetProperty(ref _year, value))
                {
                    OnPropertyChanged(nameof(YearDisplay));
                }
            }
        }

        public string YearDisplay => Year.ToString();

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }

        public string BackgroundColor => IsSelected ? "Green" : "Transparent";
        public string TextColor => IsSelected ? "White" : "#FFFFFF";
    }
}
