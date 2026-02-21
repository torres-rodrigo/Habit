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

        public ObservableCollection<HabitStatistics> HabitStatistics { get; set; }
        public TaskStatistics TaskStatistics { get; set; } = null!;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public StatisticsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            HabitStatistics = new ObservableCollection<HabitStatistics>();
            _ = LoadStatisticsAsync();
        }

        public async Task LoadStatisticsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                HabitStatistics.Clear();
                var habitStats = await _dataService.GetAllHabitStatisticsAsync();
                foreach (var stat in habitStats)
                {
                    HabitStatistics.Add(stat);
                }

                TaskStatistics = await _dataService.GetTaskStatisticsAsync();
                OnPropertyChanged(nameof(TaskStatistics));
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
