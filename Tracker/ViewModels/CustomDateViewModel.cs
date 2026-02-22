using System;
using System.Windows.Input;

namespace Tracker.ViewModels
{
    public class CustomDateViewModel : BaseViewModel
    {
        private string _yearText = string.Empty;
        private DateTime? _selectedMonth;
        private DateTime? _selectedWeekStart;
        private DateTime? _selectedDay;
        private bool _isYearEnabled = true;
        private bool _isMonthEnabled = true;
        private bool _isWeekEnabled = true;
        private bool _isDayEnabled = true;
        private bool _isMonthSet = false;
        private bool _isWeekSet = false;
        private bool _isDaySet = false;

        public string YearText
        {
            get => _yearText;
            set
            {
                if (SetProperty(ref _yearText, value))
                {
                    UpdateEnabledStates();
                }
            }
        }

        public DateTime? SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    if (value.HasValue && !_isMonthSet)
                    {
                        _isMonthSet = true;
                    }
                    UpdateEnabledStates();
                }
            }
        }

        public DateTime? SelectedWeekStart
        {
            get => _selectedWeekStart;
            set
            {
                if (SetProperty(ref _selectedWeekStart, value))
                {
                    if (value.HasValue && !_isWeekSet)
                    {
                        _isWeekSet = true;
                    }
                    UpdateEnabledStates();
                }
            }
        }

        public DateTime? SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (SetProperty(ref _selectedDay, value))
                {
                    if (value.HasValue && !_isDaySet)
                    {
                        _isDaySet = true;
                    }
                    UpdateEnabledStates();
                }
            }
        }

        public bool IsYearEnabled
        {
            get => _isYearEnabled;
            set => SetProperty(ref _isYearEnabled, value);
        }

        public bool IsMonthEnabled
        {
            get => _isMonthEnabled;
            set => SetProperty(ref _isMonthEnabled, value);
        }

        public bool IsWeekEnabled
        {
            get => _isWeekEnabled;
            set => SetProperty(ref _isWeekEnabled, value);
        }

        public bool IsDayEnabled
        {
            get => _isDayEnabled;
            set => SetProperty(ref _isDayEnabled, value);
        }

        public ICommand ClearYearCommand { get; }
        public ICommand ClearMonthCommand { get; }
        public ICommand ClearWeekCommand { get; }
        public ICommand ClearDayCommand { get; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public CustomDateViewModel()
        {
            Title = "Custom Date";

            ClearYearCommand = new Command(ClearYear);
            ClearMonthCommand = new Command(ClearMonth);
            ClearWeekCommand = new Command(ClearWeek);
            ClearDayCommand = new Command(ClearDay);
            ConfirmCommand = new Command(async () => await ConfirmAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        private void UpdateEnabledStates()
        {
            // Mutually exclusive logic
            if (!string.IsNullOrWhiteSpace(YearText))
            {
                IsMonthEnabled = false;
                IsWeekEnabled = false;
                IsDayEnabled = false;
            }
            else if (_isMonthSet && SelectedMonth.HasValue)
            {
                IsYearEnabled = false;
                IsWeekEnabled = false;
                IsDayEnabled = false;
            }
            else if (_isWeekSet && SelectedWeekStart.HasValue)
            {
                IsYearEnabled = false;
                IsMonthEnabled = false;
                IsDayEnabled = false;
            }
            else if (_isDaySet && SelectedDay.HasValue)
            {
                IsYearEnabled = false;
                IsMonthEnabled = false;
                IsWeekEnabled = false;
            }
            else
            {
                // All cleared, enable all
                IsYearEnabled = true;
                IsMonthEnabled = true;
                IsWeekEnabled = true;
                IsDayEnabled = true;
            }
        }

        private void ClearYear()
        {
            YearText = string.Empty;
        }

        private void ClearMonth()
        {
            _isMonthSet = false;
            SelectedMonth = null;
        }

        private void ClearWeek()
        {
            _isWeekSet = false;
            SelectedWeekStart = null;
        }

        private void ClearDay()
        {
            _isDaySet = false;
            SelectedDay = null;
        }

        public void ResetAllSelections()
        {
            YearText = string.Empty;
            _isMonthSet = false;
            SelectedMonth = null;
            _isWeekSet = false;
            SelectedWeekStart = null;
            _isDaySet = false;
            SelectedDay = null;

            // Re-enable all options
            IsYearEnabled = true;
            IsMonthEnabled = true;
            IsWeekEnabled = true;
            IsDayEnabled = true;
        }

        private async Task ConfirmAsync()
        {
            // Validate that at least one option is selected
            if (string.IsNullOrWhiteSpace(YearText) && !SelectedMonth.HasValue &&
                !SelectedWeekStart.HasValue && !SelectedDay.HasValue)
            {
                await Shell.Current.DisplayAlert("Validation", "Please select a date option", "OK");
                return;
            }

            DateTime startDate;
            DateTime endDate;
            string displayText;

            if (!string.IsNullOrWhiteSpace(YearText))
            {
                // Year only: yyyy
                if (!int.TryParse(YearText, out int year) || year < 1900 || year > 2100)
                {
                    await Shell.Current.DisplayAlert("Validation", "Please enter a valid year (1900-2100)", "OK");
                    return;
                }
                startDate = new DateTime(year, 1, 1);
                endDate = new DateTime(year, 12, 31, 23, 59, 59);
                displayText = year.ToString();
            }
            else if (SelectedMonth.HasValue)
            {
                // Month: mm/yyyy
                var month = SelectedMonth.Value;
                startDate = new DateTime(month.Year, month.Month, 1);
                endDate = startDate.AddMonths(1).AddTicks(-1);
                displayText = month.ToString("MM/yyyy");
            }
            else if (SelectedWeekStart.HasValue)
            {
                // Week: week start - week end mm/yyyy
                var weekStart = SelectedWeekStart.Value;
                startDate = weekStart;
                endDate = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                var weekEnd = weekStart.AddDays(6);
                displayText = $"{weekStart.Day} - {weekEnd.Day} {weekStart:MM/yyyy}";
            }
            else if (SelectedDay.HasValue)
            {
                // Day: dd/mm/yyyy
                var day = SelectedDay.Value;
                startDate = day.Date;
                endDate = day.Date.AddDays(1).AddTicks(-1);
                displayText = day.ToString("dd/MM/yyyy");
            }
            else
            {
                // Should never reach here due to validation above
                return;
            }

            // Pass the result back to TaskViewModel
            // We'll need to get the TaskViewModel instance somehow
            // For now, use MessagingCenter or pass it through navigation parameters
#pragma warning disable CS0618 // Type or member is obsolete
            MessagingCenter.Send(this, "CustomDateSelected", new CustomDateResult
            {
                StartDate = startDate,
                EndDate = endDate,
                DisplayText = displayText
            });
#pragma warning restore CS0618 // Type or member is obsolete

            await Task.CompletedTask;
        }

        private async Task CancelAsync()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MessagingCenter.Send(this, "CustomDateCancelled");
#pragma warning restore CS0618 // Type or member is obsolete
            await Task.CompletedTask;
        }
    }

    public class CustomDateResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }
}
