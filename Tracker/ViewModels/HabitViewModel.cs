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
        private bool _showCompleted;
        private bool _activeHabitsExpanded = false;  // false = expanded (to match TasksPage pattern)
        private bool _completedHabitsExpanded = false;  // false = expanded (to match TasksPage pattern)

        public ObservableCollection<HabitCardViewModel> Habits { get; set; }
        public ObservableCollection<HabitCardViewModel> ActiveHabits { get; set; }
        public ObservableCollection<HabitCardViewModel> CompletedHabits { get; set; }

        public ICommand AddHabitCommand { get; }
        public ICommand EditHabitCommand { get; }
        public ICommand DeleteHabitCommand { get; }
        public ICommand ToggleCompletionCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand NavigateToHabitCommand { get; }
        public ICommand NavigateToTaskCommand { get; }
        public ICommand ToggleActiveHabitsSectionCommand { get; }
        public ICommand ToggleCompletedHabitsSectionCommand { get; }

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

        public bool ShowCompleted
        {
            get => _showCompleted;
            set
            {
                if (SetProperty(ref _showCompleted, value))
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

        public bool HasCompletedHabits => CompletedHabits.Count > 0;

        public HabitViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Habits = new ObservableCollection<HabitCardViewModel>();
            ActiveHabits = new ObservableCollection<HabitCardViewModel>();
            CompletedHabits = new ObservableCollection<HabitCardViewModel>();

            AddHabitCommand = new Command(OnAddHabit);
            EditHabitCommand = new Command<Guid>(async (id) => await OnEditHabit(id));
            DeleteHabitCommand = new Command<Guid>(async (id) => await OnDeleteHabit(id));
            ToggleCompletionCommand = new Command<DayCompletionViewModel>(async (day) => await OnToggleCompletion(day));
            CloseOverlayCommand = new Command(() => ShowOverlay = false);
            NavigateToHabitCommand = new Command(OnNavigateToHabit);
            NavigateToTaskCommand = new Command(OnNavigateToTask);
            ToggleActiveHabitsSectionCommand = new Command(() => ActiveHabitsExpanded = !ActiveHabitsExpanded);
            ToggleCompletedHabitsSectionCommand = new Command(() => CompletedHabitsExpanded = !CompletedHabitsExpanded);

            _ = LoadHabitsAsync();
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
                    Habits.Add(new HabitCardViewModel(habit, _dataService));
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

            var now = DateTime.Now;

            foreach (var habit in Habits)
            {
                // A habit is completed if it has a deadline that has passed
                var isCompleted = habit.Deadline.HasValue && habit.Deadline.Value.Date < now.Date;

                if (isCompleted)
                {
                    CompletedHabits.Add(habit);
                }
                else
                {
                    ActiveHabits.Add(habit);
                }
            }

            OnPropertyChanged(nameof(HasCompletedHabits));
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
            // Find the habit to check if it's completed
            var habitCard = Habits.FirstOrDefault(h => h.Id == habitId)
                ?? ActiveHabits.FirstOrDefault(h => h.Id == habitId)
                ?? CompletedHabits.FirstOrDefault(h => h.Id == habitId);

            if (habitCard != null && habitCard.IsCompleted)
            {
                // Show confirmation modal for completed habits
                var confirm = await Shell.Current.DisplayAlert(
                    "Edit Completed Habit",
                    "This habit is already completed. Are you sure you want to edit it? This may affect your statistics.",
                    "Edit",
                    "Cancel");

                if (!confirm) return;
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

                IsLoading = true;
                await _dataService.DeleteHabitAsync(habitId);
                await LoadHabitsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete habit: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OnToggleCompletion(DayCompletionViewModel? dayCompletion)
        {
            if (dayCompletion == null || !dayCompletion.ShouldTrack)
                return;

            // Check if the habit is completed (past deadline)
            var habitCard = Habits.FirstOrDefault(h => h.Id == dayCompletion.HabitId)
                ?? ActiveHabits.FirstOrDefault(h => h.Id == dayCompletion.HabitId)
                ?? CompletedHabits.FirstOrDefault(h => h.Id == dayCompletion.HabitId);

            if (habitCard != null && habitCard.IsCompleted)
            {
                // Prevent toggling for completed habits
                return;
            }

            try
            {
                await _dataService.ToggleHabitCompletionAsync(dayCompletion.HabitId, dayCompletion.Date);

                // Update only the toggled day and recalculate percentages
                if (habitCard != null)
                {
                    await habitCard.UpdateDayCompletionAsync(dayCompletion.Date);
                }
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
        private readonly IDataService _dataService;

        public Guid Id => _habit.Id;
        public string Name => _habit.Name;
        public string Description => _habit.Description;
        public bool IsNegativeHabit => _habit.IsNegativeHabit;
        public string HabitColor => IsNegativeHabit ? "Red" : "Green";
        public DateTime? Deadline => _habit.Deadline;
        public bool IsCompleted => Deadline.HasValue && Deadline.Value.Date < DateTime.Now.Date;
        public ObservableCollection<DayCompletionViewModel> WeekDays { get; set; }
        
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

        public HabitCardViewModel(Habit habit, IDataService dataService)
        {
            _habit = habit;
            _dataService = dataService;
            WeekDays = new ObservableCollection<DayCompletionViewModel>();
            _ = LoadWeekProgressAsync();
        }

        public async Task RefreshWeekProgressAsync()
        {
            await LoadWeekProgressAsync();
        }

        public async Task UpdateDayCompletionAsync(DateTime date)
        {
            // Find the specific day and update only its completion status
            var day = WeekDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day != null)
            {
                var isCompleted = await _dataService.IsHabitCompletedOnDateAsync(_habit.Id, date);
                day.IsCompleted = isCompleted;

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

        private async Task LoadWeekProgressAsync()
        {
            WeekDays.Clear();
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            // Calculate week number (ISO 8601)
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            WeekNumber = calendar.GetWeekOfYear(today,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Sunday);

            int completedDays = 0;
            int trackedDays = 0;

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var isCompleted = await _dataService.IsHabitCompletedOnDateAsync(_habit.Id, date);
                var shouldTrack = _habit.TrackEveryday || _habit.TrackingDays.Contains(date.DayOfWeek);

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
                    IsToday = date.Date == DateTime.Today,
                    HabitColor = HabitColor,
                    IsHabitCompleted = IsCompleted
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
    }
}
