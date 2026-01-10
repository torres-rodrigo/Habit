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

        public ObservableCollection<HabitCardViewModel> Habits { get; set; }
        public ICommand AddHabitCommand { get; }
        public ICommand EditHabitCommand { get; }
        public ICommand DeleteHabitCommand { get; }
        public ICommand ToggleCompletionCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand NavigateToHabitCommand { get; }
        public ICommand NavigateToTaskCommand { get; }

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => SetProperty(ref _showOverlay, value);
        }

        public HabitViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Habits = new ObservableCollection<HabitCardViewModel>();
            
            AddHabitCommand = new Command(OnAddHabit);
            EditHabitCommand = new Command<Guid>(OnEditHabit);
            DeleteHabitCommand = new Command<Guid>(OnDeleteHabit);
            ToggleCompletionCommand = new Command<DayCompletionViewModel>(OnToggleCompletion);
            CloseOverlayCommand = new Command(() => ShowOverlay = false);
            NavigateToHabitCommand = new Command(OnNavigateToHabit);
            NavigateToTaskCommand = new Command(OnNavigateToTask);

            LoadHabits();
        }

        private void LoadHabits()
        {
            Habits.Clear();
            var habits = _dataService.GetAllHabits();
            foreach (var habit in habits)
            {
                Habits.Add(new HabitCardViewModel(habit, _dataService));
            }
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

        private async void OnEditHabit(Guid habitId)
        {
            // Navigate to edit habit page
            await Shell.Current.GoToAsync($"habits/edithabit?id={habitId}");
        }

        private void OnDeleteHabit(Guid habitId)
        {
            _dataService.DeleteHabit(habitId);
            LoadHabits();
        }

        private void OnToggleCompletion(DayCompletionViewModel? dayCompletion)
        {
            if (dayCompletion == null || !dayCompletion.ShouldTrack)
                return;

            _dataService.ToggleHabitCompletion(dayCompletion.HabitId, dayCompletion.Date);
            
            // Update only the toggled day and recalculate percentages
            var habitCard = Habits.FirstOrDefault(h => h.Id == dayCompletion.HabitId);
            habitCard?.UpdateDayCompletion(dayCompletion.Date);
        }

        public void Refresh()
        {
            LoadHabits();
        }
    }

    public class HabitCardViewModel : BaseViewModel
    {
        private readonly Habit _habit;
        private readonly IDataService _dataService;

        public Guid Id => _habit.Id;
        public string Name => _habit.Name;
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
            LoadWeekProgress();
        }

        public void RefreshWeekProgress()
        {
            LoadWeekProgress();
        }

        public void UpdateDayCompletion(DateTime date)
        {
            // Find the specific day and update only its completion status
            var day = WeekDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day != null)
            {
                var isCompleted = _dataService.IsHabitCompletedOnDate(_habit.Id, date);
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

        private void LoadWeekProgress()
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
                var isCompleted = _dataService.IsHabitCompletedOnDate(_habit.Id, date);
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
                    IsToday = date.Date == DateTime.Today
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
        public string DayName { get; set; }
        
        private bool _isCompleted;
        public bool IsCompleted 
        { 
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
        
        public bool ShouldTrack { get; set; }
        public bool IsToday { get; set; }
    }
}
