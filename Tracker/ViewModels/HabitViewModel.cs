using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    public class HabitViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private bool _showOverlay;
        private bool _isLoading;
        private bool _showArchived;
        private bool _activeHabitsExpanded = false;  // false = expanded (to match TasksPage pattern)
        private bool _completedHabitsExpanded = false;  // false = expanded (to match TasksPage pattern)
        private bool _untrackedHabitsExpanded = false;  // false = expanded (to match TasksPage pattern)
        private bool _isReorderMode = false;
        private HabitCardViewModel? _draggedItem;

        public ObservableCollection<HabitCardViewModel> Habits { get; set; }
        public ObservableCollection<HabitCardViewModel> ActiveHabits { get; set; }
        public ObservableCollection<HabitCardViewModel> CompletedHabits { get; set; }
        public ObservableCollection<HabitCardViewModel> UntrackedHabits { get; set; }

        public ICommand AddHabitCommand { get; }
        public ICommand EditHabitCommand { get; }
        public ICommand DeleteHabitCommand { get; }
        public ICommand ToggleCompletionCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand NavigateToHabitCommand { get; }
        public ICommand NavigateToTaskCommand { get; }
        public ICommand ToggleActiveHabitsSectionCommand { get; }
        public ICommand ToggleCompletedHabitsSectionCommand { get; }
        public ICommand ToggleUntrackedHabitsSectionCommand { get; }
        public ICommand ToggleReorderModeCommand { get; }
        public ICommand StartDragCommand { get; }
        public ICommand DropCommand { get; }

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

        public bool ShowArchived
        {
            get => _showArchived;
            set
            {
                if (SetProperty(ref _showArchived, value))
                {
                    OrganizeHabits();
                }
            }
        }

        public bool ActiveHabitsExpanded
        {
            get => _activeHabitsExpanded;
            set => SetProperty(ref _activeHabitsExpanded, value);
        }

        public bool CompletedHabitsExpanded
        {
            get => _completedHabitsExpanded;
            set => SetProperty(ref _completedHabitsExpanded, value);
        }

        public bool UntrackedHabitsExpanded
        {
            get => _untrackedHabitsExpanded;
            set => SetProperty(ref _untrackedHabitsExpanded, value);
        }

        public bool IsReorderMode
        {
            get => _isReorderMode;
            set
            {
                if (SetProperty(ref _isReorderMode, value))
                {
                    OnPropertyChanged(nameof(ReorderButtonText));
                }
            }
        }

        public string ReorderButtonText => IsReorderMode ? "Done" : "Reorder";

        public bool HasCompletedHabits => CompletedHabits.Count > 0;
        public bool HasUntrackedHabits => UntrackedHabits.Count > 0;

        public HabitViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Habits = new ObservableCollection<HabitCardViewModel>();
            ActiveHabits = new ObservableCollection<HabitCardViewModel>();
            CompletedHabits = new ObservableCollection<HabitCardViewModel>();
            UntrackedHabits = new ObservableCollection<HabitCardViewModel>();

            AddHabitCommand = new Command(OnAddHabit);
            EditHabitCommand = new Command<Guid>(async (id) => await OnEditHabit(id));
            DeleteHabitCommand = new Command<Guid>(async (id) => await OnDeleteHabit(id));
            ToggleCompletionCommand = new Command<DayCompletionViewModel>(async (day) => await OnToggleCompletion(day));
            CloseOverlayCommand = new Command(() => ShowOverlay = false);
            NavigateToHabitCommand = new Command(OnNavigateToHabit);
            NavigateToTaskCommand = new Command(OnNavigateToTask);
            ToggleActiveHabitsSectionCommand = new Command(() => ActiveHabitsExpanded = !ActiveHabitsExpanded);
            ToggleCompletedHabitsSectionCommand = new Command(() => CompletedHabitsExpanded = !CompletedHabitsExpanded);
            ToggleUntrackedHabitsSectionCommand = new Command(() => UntrackedHabitsExpanded = !UntrackedHabitsExpanded);
            ToggleReorderModeCommand = new Command(async () => await OnToggleReorderModeAsync());
            StartDragCommand = new Command<HabitCardViewModel>(OnStartDrag);
            DropCommand = new Command<HabitCardViewModel>(OnDrop);

            RunAsync(LoadHabitsAsync);
        }

        private async Task LoadHabitsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                Habits.Clear();
                var habits = await _dataService.GetAllHabitsAsync();
                foreach (var habit in habits)
                {
                    Habits.Add(new HabitCardViewModel(habit));
                }
                OrganizeHabits();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load habits: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OrganizeHabits()
        {
            ActiveHabits.Clear();
            CompletedHabits.Clear();
            UntrackedHabits.Clear();

            var now = DateTime.Now;

            // Categorize and sort active habits in a single pass
            var activeList = new List<HabitCardViewModel>();

            foreach (var habit in Habits)
            {
                if (!habit.IsTracked)
                {
                    UntrackedHabits.Add(habit);
                }
                else if (habit.Deadline.HasValue && habit.Deadline.Value.Date < now.Date)
                {
                    CompletedHabits.Add(habit);
                }
                else
                {
                    activeList.Add(habit);
                }
            }

            // Sort active habits once, then add to the ObservableCollection
            activeList.Sort((a, b) =>
            {
                var priorityCompare = GetSortPriority(a).CompareTo(GetSortPriority(b));
                return priorityCompare != 0 ? priorityCompare : a.DisplayOrder.CompareTo(b.DisplayOrder);
            });

            foreach (var habit in activeList)
            {
                ActiveHabits.Add(habit);
            }

            OnPropertyChanged(nameof(HasCompletedHabits));
            OnPropertyChanged(nameof(HasUntrackedHabits));
        }

        private int GetSortPriority(HabitCardViewModel habit)
        {
            // Priority 1: Tracked today and not completed
            if (habit.IsTrackedToday && !habit.IsCompletedToday)
                return 1;

            // Priority 2: Tracked today and completed
            if (habit.IsTrackedToday && habit.IsCompletedToday)
                return 2;

            // Priority 3: Not tracked today
            return 3;
        }

        private void OnAddHabit()
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

        private async Task OnEditHabit(Guid habitId)
        {
            // In reorder mode, use tap to select for reordering
            if (IsReorderMode)
            {
                var habit = ActiveHabits.FirstOrDefault(h => h.Id == habitId);
                if (habit != null)
                {
                    OnStartDrag(habit);
                }
                return;
            }

            // Find the habit to check if it's completed or untracked
            var habitCard = Habits.FirstOrDefault(h => h.Id == habitId);

            if (habitCard != null)
            {
                // Show confirmation modal for untracked habits
                if (!habitCard.IsTracked)
                {
                    var confirm = await Shell.Current.DisplayAlert(
                        "Edit Untracked Habit",
                        "This habit is untracked. Are you sure you want to edit it?",
                        "Edit",
                        "Cancel");

                    if (!confirm) return;
                }
                // Show confirmation modal for completed habits
                else if (habitCard.IsCompleted)
                {
                    var confirm = await Shell.Current.DisplayAlert(
                        "Edit Completed Habit",
                        "This habit is already completed. Are you sure you want to edit it? This may affect your statistics.",
                        "Edit",
                        "Cancel");

                    if (!confirm) return;
                }
            }

            // Navigate to edit habit page
            await Shell.Current.GoToAsync($"habits/edithabit?id={habitId}");
        }

        private async Task OnDeleteHabit(Guid habitId)
        {
            if (IsLoading) return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Delete Habit",
                    "Are you sure you want to delete this habit? This action cannot be undone.",
                    "Delete",
                    "Cancel");

                if (!confirm) return;

                await _dataService.DeleteHabitAsync(habitId);
                await LoadHabitsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete habit: {ex.Message}", "OK");
            }
        }

        private async Task OnToggleReorderModeAsync()
        {
            if (IsReorderMode)
            {
                // Exiting reorder mode - save the new DisplayOrder values
                try
                {
                    IsLoading = true;

                    // Update DisplayOrder for all active habits based on their current position
                    for (int i = 0; i < ActiveHabits.Count; i++)
                    {
                        var habit = ActiveHabits[i];
                        if (habit.DisplayOrder != i)
                        {
                            await _dataService.UpdateHabitDisplayOrderAsync(habit.Id, i);
                            habit.UpdateDisplayOrder(i);
                        }
                    }

                    IsReorderMode = false;
                    // Re-apply auto-sorting after saving
                    OrganizeHabits();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to save order: {ex.Message}", "OK");
                }
                finally
                {
                    IsLoading = false;
                }
            }
            else
            {
                // Entering reorder mode - show habits sorted by DisplayOrder only (ignore auto-sorting groups)
                var sortedByDisplayOrder = ActiveHabits
                    .OrderBy(h => h.DisplayOrder)
                    .ToList();

                ActiveHabits.Clear();
                foreach (var habit in sortedByDisplayOrder)
                {
                    ActiveHabits.Add(habit);
                }

                IsReorderMode = true;
            }
        }

        private void OnStartDrag(HabitCardViewModel? item)
        {
            if (item == null || !IsReorderMode) return;

            // If no item is selected, select this one
            if (_draggedItem == null)
            {
                _draggedItem = item;
                item.IsSelectedForReorder = true;
            }
            // If an item is already selected, swap positions
            else
            {
                OnDrop(item);
            }
        }

        private void OnDrop(HabitCardViewModel? targetItem)
        {
            if (targetItem == null || _draggedItem == null || !IsReorderMode) return;
            if (_draggedItem == targetItem)
            {
                // Deselect if same item clicked again
                _draggedItem.IsSelectedForReorder = false;
                _draggedItem = null;
                return;
            }

            var oldIndex = ActiveHabits.IndexOf(_draggedItem);
            var newIndex = ActiveHabits.IndexOf(targetItem);

            if (oldIndex != -1 && newIndex != -1)
            {
                // Remove and insert at new position
                ActiveHabits.RemoveAt(oldIndex);
                ActiveHabits.Insert(newIndex, _draggedItem);
            }

            _draggedItem.IsSelectedForReorder = false;
            _draggedItem = null;
        }

        private async Task OnToggleCompletion(DayCompletionViewModel? dayCompletion)
        {
            // Don't allow toggling completion while in reorder mode
            if (IsReorderMode) return;

            if (dayCompletion == null || !dayCompletion.CanToggle)
                return;

            // Check if the habit is completed (past deadline) or untracked
            var habitCard = Habits.FirstOrDefault(h => h.Id == dayCompletion.HabitId);

            if (habitCard != null)
            {
                // Prevent toggling for untracked habits
                if (!habitCard.IsTracked)
                {
                    return;
                }

                // Prevent toggling for completed habits
                if (habitCard.IsCompleted)
                {
                    return;
                }
            }

            try
            {
                await _dataService.ToggleHabitCompletionAsync(dayCompletion.HabitId, dayCompletion.Date);

                // Update only the toggled day and recalculate percentages
                if (habitCard != null)
                {
                    habitCard.UpdateDayCompletion(dayCompletion.Date);
                }

                // Re-apply auto-sorting since completion status changed
                OrganizeHabits();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to toggle completion: {ex.Message}", "OK");
            }
        }

        public async Task RefreshAsync()
        {
            await LoadHabitsAsync();
        }
    }

    public class HabitCardViewModel : BaseViewModel
    {
        private readonly Habit _habit;

        public Guid Id => _habit.Id;
        public string Name => _habit.Name;
        public string Description => _habit.Description;
        public bool IsNegativeHabit => _habit.IsNegativeHabit;
        public bool IsTracked => _habit.IsTracked;
        public string HabitColor
        {
            get
            {
                if (!IsTracked) return "#FFC107"; // Yellow for untracked
                return IsNegativeHabit ? "Red" : "Green";
            }
        }
        public DateTime? Deadline => _habit.Deadline;
        public bool IsCompleted => Deadline.HasValue && Deadline.Value.Date < DateTime.Now.Date;
        public string UntrackedDateFormatted => _habit.UntrackedDate?.ToString("dd/MM/yyyy") ?? string.Empty;
        public ObservableCollection<DayCompletionViewModel> WeekDays { get; set; }

        private int _displayOrder;
        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }

        private bool _isSelectedForReorder;
        public bool IsSelectedForReorder
        {
            get => _isSelectedForReorder;
            set
            {
                if (SetProperty(ref _isSelectedForReorder, value))
                {
                    OnPropertyChanged(nameof(SelectionBackgroundColor));
                }
            }
        }

        public Color SelectionBackgroundColor => IsSelectedForReorder ? Color.FromArgb("#E0E0E0") : Colors.Transparent;

        public bool IsTrackedToday
        {
            get
            {
                return _habit.ShouldTrackOnDay(DateTime.Today);
            }
        }

        public bool IsCompletedToday
        {
            get
            {
                // Check completions directly from the habit, not from WeekDays (which loads async)
                var today = DateTime.Today;
                return _habit.Completions.Any(c => c.CompletedDate.Date == today);
            }
        }
        
        private int _weekNumber;
        public int WeekNumber 
        { 
            get => _weekNumber;
            private set => SetProperty(ref _weekNumber, value);
        }
        
        private string _weeklyCompletionPercentage = "0%";
        public string WeeklyCompletionPercentage 
        { 
            get => _weeklyCompletionPercentage;
            private set => SetProperty(ref _weeklyCompletionPercentage, value);
        }
        
        private double _weeklyCompletionDecimal = 0.0;
        public double WeeklyCompletionDecimal 
        { 
            get => _weeklyCompletionDecimal;
            private set => SetProperty(ref _weeklyCompletionDecimal, value);
        }

        public HabitCardViewModel(Habit habit)
        {
            _habit = habit;
            _displayOrder = habit.DisplayOrder;
            WeekDays = new ObservableCollection<DayCompletionViewModel>();
            LoadWeekProgress();
        }

        public void UpdateDisplayOrder(int newOrder)
        {
            DisplayOrder = newOrder;
        }

        public void RefreshWeekProgress()
        {
            LoadWeekProgress();
        }

        public void UpdateDayCompletion(DateTime date)
        {
            // Find the specific day and toggle its completion status
            var day = WeekDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day != null)
            {
                var dateOnly = date.Date;
                var existingCompletion = _habit.Completions.FirstOrDefault(c => c.CompletedDate.Date == dateOnly);

                if (existingCompletion != null)
                {
                    // Was completed, now uncompleted
                    _habit.Completions.Remove(existingCompletion);
                    day.IsCompleted = false;
                }
                else
                {
                    // Was not completed, now completed
                    _habit.Completions.Add(new HabitCompletion
                    {
                        Id = Guid.NewGuid(),
                        HabitId = _habit.Id,
                        CompletedDate = dateOnly
                    });
                    day.IsCompleted = true;
                }

                // Recalculate percentages without rebuilding the collection
                RecalculateWeeklyCompletion();
            }
        }

        private void RecalculateWeeklyCompletion()
        {
            var trackedDays = WeekDays.Where(d => d.ShouldTrack).ToList();
            var completedDays = trackedDays.Count(d => d.IsCompleted);

            UpdateCompletionPercentage(completedDays, trackedDays.Count);
        }

        private void UpdateCompletionPercentage(int completed, int total)
        {
            if (total > 0)
            {
                var percentage = (int)Math.Round((double)completed / total * 100);
                WeeklyCompletionPercentage = $"{percentage}%";
                WeeklyCompletionDecimal = (double)completed / total;
            }
            else
            {
                WeeklyCompletionPercentage = "0%";
                WeeklyCompletionDecimal = 0.0;
            }
        }

        private void LoadWeekProgress()
        {
            WeekDays.Clear();

            // Determine which week to display based on habit status
            DateTime referenceDate;
            if (!IsTracked && _habit.UntrackedDate.HasValue)
            {
                // Untracked habit: show week when it was untracked
                referenceDate = _habit.UntrackedDate.Value.Date;
            }
            else if (IsCompleted && _habit.Deadline.HasValue)
            {
                // Completed habit: show week when deadline passed
                referenceDate = _habit.Deadline.Value.Date;
            }
            else
            {
                // Active habit: show current week
                referenceDate = DateTime.Today;
            }

            var startOfWeek = referenceDate.AddDays(-(int)referenceDate.DayOfWeek);

            // Calculate week number (ISO 8601)
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            WeekNumber = calendar.GetWeekOfYear(referenceDate,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Sunday);

            // Build HashSet for O(1) completion lookups from already-loaded data
            var completionDates = _habit.Completions.Select(c => c.CompletedDate.Date).ToHashSet();

            // Hoist invariant lookup out of the loop
            var currentPeriodStart = _habit.TrackingPeriods
                .FirstOrDefault(p => p.EndDate == null)?.StartDate;

            int completedDays = 0;
            int trackedDays = 0;

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var isCompleted = completionDates.Contains(date.Date);
                var shouldTrack = _habit.ShouldTrackOnDay(date);

                if (shouldTrack)
                {
                    trackedDays++;
                    if (isCompleted)
                    {
                        completedDays++;
                    }
                }

                WeekDays.Add(new DayCompletionViewModel
                {
                    HabitId = _habit.Id,
                    Date = date,
                    DayName = date.ToString("ddd"),
                    IsCompleted = isCompleted,
                    ShouldTrack = shouldTrack,
                    IsToday = date.Date == referenceDate.Date,
                    HabitColor = HabitColor,
                    IsHabitCompleted = IsCompleted,
                    HabitCreatedDate = _habit.CreatedDate,
                    CurrentPeriodStartDate = currentPeriodStart
                });
            }

            // Calculate percentage
            UpdateCompletionPercentage(completedDays, trackedDays);
        }
    }

    public class DayCompletionViewModel : BaseViewModel
    {
        public Guid HabitId { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string HabitColor { get; set; } = "Green";
        public DateTime HabitCreatedDate { get; set; }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                SetProperty(ref _isCompleted, value);
                OnPropertyChanged(nameof(CompletionBackgroundColor));
            }
        }

        public string CompletionBackgroundColor => IsCompleted ? HabitColor : "White";
        public bool ShouldTrack { get; set; }
        public bool IsToday { get; set; }
        public bool IsHabitCompleted { get; set; } // Indicates if the parent habit is completed (past deadline)
        public DateTime? CurrentPeriodStartDate { get; set; }
        public bool IsInCurrentPeriod => !CurrentPeriodStartDate.HasValue || Date.Date >= CurrentPeriodStartDate.Value.Date;
        public bool IsWithinValidRange
        {
            get
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(6);
                return Date.Date >= HabitCreatedDate.Date && Date.Date <= endOfWeek;
            }
        }
        public bool CanToggle => ShouldTrack && IsWithinValidRange && !IsHabitCompleted && IsInCurrentPeriod;
        public double DayOpacity
        {
            get
            {
                if (!ShouldTrack) return 0.3;
                if (!IsWithinValidRange) return 0.4;
                if (!IsInCurrentPeriod) return 0.5;
                return 1.0;
            }
        }
    }
}
