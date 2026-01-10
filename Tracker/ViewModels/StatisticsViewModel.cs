using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<HabitStatistics> HabitStatistics { get; set; }
        public TaskStatistics TaskStatistics { get; set; }

        public StatisticsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            HabitStatistics = new ObservableCollection<HabitStatistics>();
            LoadStatistics();
        }

        public void LoadStatistics()
        {
            HabitStatistics.Clear();
            var habitStats = _dataService.GetAllHabitStatistics();
            foreach (var stat in habitStats)
            {
                HabitStatistics.Add(stat);
            }

            TaskStatistics = _dataService.GetTaskStatistics();
            OnPropertyChanged(nameof(TaskStatistics));
        }

        public void Refresh()
        {
            LoadStatistics();
        }
    }
}
