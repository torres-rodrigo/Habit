using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Models;
using Tracker.Services;

namespace Tracker.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly IDialogService _dialogService;
        private bool _isLoading;
        private bool _isTaskStatisticsCollapsed = false;
        private bool _isHabitStatisticsCollapsed = false;
        private bool _isCompletedHabitsCollapsed = false;
        private bool _isUntrackedHabitsCollapsed = false;
        private bool _showExportPopup = false;
        private string _exportStatusMessage = string.Empty;
        private bool _isExporting = false;
        private bool _isExportSuccessful = false;
        private CancellationTokenSource? _cancellationTokenSource;

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

        public bool ShowExportPopup
        {
            get => _showExportPopup;
            set => SetProperty(ref _showExportPopup, value);
        }

        public string ExportStatusMessage
        {
            get => _exportStatusMessage;
            set
            {
                if (SetProperty(ref _exportStatusMessage, value))
                {
                    OnPropertyChanged(nameof(StatusMessageColor));
                }
            }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                if (SetProperty(ref _isExporting, value))
                {
                    OnPropertyChanged(nameof(IsNotExporting));
                    OnPropertyChanged(nameof(CanExport));
                }
            }
        }

        public bool IsExportSuccessful
        {
            get => _isExportSuccessful;
            set
            {
                if (SetProperty(ref _isExportSuccessful, value))
                {
                    OnPropertyChanged(nameof(ExportButtonText));
                }
            }
        }

        public bool IsNotExporting => !IsExporting;
        public bool CanExport => !IsExporting;
        public string ExportButtonText => IsExportSuccessful ? "OK" : "Export";
        public Color StatusMessageColor
        {
            get
            {
                if (ExportStatusMessage.StartsWith("SUCCESS"))
                    return Colors.Green;
                if (ExportStatusMessage.StartsWith("ERROR"))
                    return Colors.Red;
                return Colors.White;
            }
        }

        public ICommand ToggleTaskStatisticsCollapseCommand { get; }
        public ICommand ToggleHabitStatisticsCollapseCommand { get; }
        public ICommand ToggleCompletedHabitsCollapseCommand { get; }
        public ICommand ToggleUntrackedHabitsCollapseCommand { get; }
        public ICommand ShowExportPopupCommand { get; }
        public ICommand CancelExportCommand { get; }
        public ICommand ExecuteExportCommand { get; }

        public StatisticsViewModel(IDataService dataService, IDialogService dialogService)
        {
            _dataService = dataService;
            _dialogService = dialogService;
            ActiveHabitStatistics = new ObservableCollection<HabitStatistics>();
            CompletedHabitStatistics = new ObservableCollection<HabitStatistics>();
            UntrackedHabitsByYear = new ObservableCollection<YearUntrackedCount>();
            ToggleTaskStatisticsCollapseCommand = new Command(() => IsTaskStatisticsCollapsed = !IsTaskStatisticsCollapsed);
            ToggleHabitStatisticsCollapseCommand = new Command(() => IsHabitStatisticsCollapsed = !IsHabitStatisticsCollapsed);
            ToggleCompletedHabitsCollapseCommand = new Command(() => IsCompletedHabitsCollapsed = !IsCompletedHabitsCollapsed);
            ToggleUntrackedHabitsCollapseCommand = new Command(() => IsUntrackedHabitsCollapsed = !IsUntrackedHabitsCollapsed);
            ShowExportPopupCommand = new Command(OnShowExportPopup);
            CancelExportCommand = new Command(OnCancelExport);
            ExecuteExportCommand = new Command(async () => await OnExecuteExportAsync());
            RunAsync(LoadStatisticsAsync);
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
                await _dialogService.DisplayAlertAsync("Error", $"Failed to load statistics: {ex.Message}", "OK");
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

        private void OnShowExportPopup()
        {
            ExportStatusMessage = string.Empty;
            IsExporting = false;
            IsExportSuccessful = false;
            ShowExportPopup = true;
        }

        private void OnCancelExport()
        {
            _cancellationTokenSource?.Cancel();
            ShowExportPopup = false;
            IsExporting = false;
            IsExportSuccessful = false;
            ExportStatusMessage = string.Empty;
        }

        private async Task OnExecuteExportAsync()
        {
            // If export was successful, just close the popup
            if (IsExportSuccessful)
            {
                ShowExportPopup = false;
                IsExportSuccessful = false;
                ExportStatusMessage = string.Empty;
                return;
            }

            if (IsExporting) return;

            try
            {
                IsExporting = true;
                _cancellationTokenSource = new CancellationTokenSource();
                ExportStatusMessage = "Please wait...";

                // Get the actual database path (it's tracker.db in LocalApplicationData)
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "tracker.db");

                // Check if database exists
                if (!File.Exists(dbPath))
                {
                    ExportStatusMessage = "ERROR: Database file not found";
                    return;
                }

                // Export to Documents folder where user can access it
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var backupFileName = $"tracker-{DateTime.Now:yyyy-MM-dd}-bak.db";
                var targetPath = Path.Combine(documentsPath, backupFileName);

                // Copy the database file
                await Task.Run(() =>
                {
                    File.Copy(dbPath, targetPath, overwrite: true);
                }, _cancellationTokenSource.Token);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ExportStatusMessage = string.Empty;
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                }
                else
                {
                    ExportStatusMessage = $"SUCCESS! DB exported to {targetPath}";
                    IsExportSuccessful = true;
                }
            }
            catch (OperationCanceledException)
            {
                ExportStatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"ERROR: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
