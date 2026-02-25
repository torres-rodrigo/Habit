using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private bool _isLoading;
        private bool _isTaskStatisticsCollapsed = false;
        private bool _isHabitStatisticsCollapsed = false;
        private bool _isCompletedHabitsCollapsed = false;
        private bool _isUntrackedHabitsCollapsed = false;

        public ObservableCollection<HabitStatistics> ActiveHabitStatistics { get; set; }
        public ObservableCollection<HabitStatistics> CompletedHabitStatistics { get; set; }
        public ObservableCollection<YearUntrackedCount> UntrackedHabitsByYear { get; set; }
        public TaskStatistics TaskStatistics { get; set; } = null!;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsTaskStatisticsCollapsed
        {
            get => _isTaskStatisticsCollapsed;
            set => SetProperty(ref _isTaskStatisticsCollapsed, value);
        }

        public bool IsHabitStatisticsCollapsed
        {
            get => _isHabitStatisticsCollapsed;
            set => SetProperty(ref _isHabitStatisticsCollapsed, value);
        }

        public bool IsCompletedHabitsCollapsed
        {
            get => _isCompletedHabitsCollapsed;
            set => SetProperty(ref _isCompletedHabitsCollapsed, value);
        }

        public bool IsUntrackedHabitsCollapsed
        {
            get => _isUntrackedHabitsCollapsed;
            set => SetProperty(ref _isUntrackedHabitsCollapsed, value);
        }

        public bool HasCompletedHabits => CompletedHabitStatistics.Count > 0;
        public bool HasUntrackedHabits => UntrackedHabitsByYear.Count > 0;

        public ICommand ToggleTaskStatisticsCollapseCommand { get; }
        public ICommand ToggleHabitStatisticsCollapseCommand { get; }
        public ICommand ToggleCompletedHabitsCollapseCommand { get; }
        public ICommand ToggleUntrackedHabitsCollapseCommand { get; }

        public StatisticsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            ActiveHabitStatistics = new ObservableCollection<HabitStatistics>();
            CompletedHabitStatistics = new ObservableCollection<HabitStatistics>();
            UntrackedHabitsByYear = new ObservableCollection<YearUntrackedCount>();
            ToggleTaskStatisticsCollapseCommand = new Command(() => IsTaskStatisticsCollapsed = !IsTaskStatisticsCollapsed);
            ToggleHabitStatisticsCollapseCommand = new Command(() => IsHabitStatisticsCollapsed = !IsHabitStatisticsCollapsed);
            ToggleCompletedHabitsCollapseCommand = new Command(() => IsCompletedHabitsCollapsed = !IsCompletedHabitsCollapsed);
            ToggleUntrackedHabitsCollapseCommand = new Command(() => IsUntrackedHabitsCollapsed = !IsUntrackedHabitsCollapsed);
            _ = LoadStatisticsAsync();
        }

        public async Task LoadStatisticsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ActiveHabitStatistics.Clear();
                CompletedHabitStatistics.Clear();
                UntrackedHabitsByYear.Clear();

                // Get all habits
                var allHabits = await _dataService.GetAllHabitsAsync();
                var now = DateTime.Now;

                // Separate habits into active, completed, and untracked
                var activeHabits = new List<Habit>();
                var completedHabits = new List<Habit>();
                var untrackedHabits = new List<Habit>();

                foreach (var habit in allHabits)
                {
                    if (!habit.IsTracked)
                    {
                        untrackedHabits.Add(habit);
                    }
                    else if (habit.Deadline.HasValue && habit.Deadline.Value.Date < now.Date)
                    {
                        completedHabits.Add(habit);
                    }
                    else
                    {
                        activeHabits.Add(habit);
                    }
                }

                // Load statistics for active habits
                foreach (var habit in activeHabits)
                {
                    var stat = await _dataService.GetHabitStatisticsAsync(habit.Id);
                    if (stat != null)
                    {
                        ActiveHabitStatistics.Add(stat);
                    }
                }

                // Load statistics for completed habits
                foreach (var habit in completedHabits)
                {
                    var stat = await _dataService.GetHabitStatisticsAsync(habit.Id);
                    if (stat != null)
                    {
                        CompletedHabitStatistics.Add(stat);
                    }
                }

                // Group untracked habits by year and calculate totals
                var habitsByYear = allHabits
                    .GroupBy(h => h.CreatedDate.Year)
                    .ToDictionary(g => g.Key, g => g.Count());

                var untrackedByYear = untrackedHabits
                    .GroupBy(h => (h.UntrackedDate ?? h.CreatedDate).Year)
                    .Select(g => new YearUntrackedCount
                    {
                        Year = g.Key,
                        UntrackedCount = g.Count(),
                        TotalCount = habitsByYear.ContainsKey(g.Key) ? habitsByYear[g.Key] : g.Count()
                    })
                    .OrderByDescending(y => y.Year)
                    .ToList();

                foreach (var yearCount in untrackedByYear)
                {
                    UntrackedHabitsByYear.Add(yearCount);
                }

                // Load task statistics
                TaskStatistics = await _dataService.GetTaskStatisticsAsync();
                OnPropertyChanged(nameof(TaskStatistics));
                OnPropertyChanged(nameof(HasCompletedHabits));
                OnPropertyChanged(nameof(HasUntrackedHabits));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load statistics: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadStatisticsAsync();
        }
    }
}
