using Tracker.ViewModels;

namespace Tracker.Views;

public partial class CustomDatePopup : Grid
{
    private CustomDateViewModel? _viewModel;

    // Parameterless constructor for XAML instantiation
    public CustomDatePopup()
    {
        InitializeComponent();
    }

    // Constructor with DI for code-behind usage
    public CustomDatePopup(CustomDateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnMonthPickerClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as CustomDateViewModel;
        if (viewModel == null) return;

        var result = await Shell.Current.DisplayPromptAsync(
            "Select Month",
            "Enter month and year (MM/YYYY)",
            placeholder: "01/2024",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && DateTime.TryParseExact(result, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            viewModel.SelectedMonth = date;
        }
    }

    private async void OnWeekPickerClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as CustomDateViewModel;
        if (viewModel == null) return;

        var result = await Shell.Current.DisplayPromptAsync(
            "Select Week Start",
            "Enter week start date (DD/MM/YYYY)",
            placeholder: "15/02/2024",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && DateTime.TryParseExact(result, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            viewModel.SelectedWeekStart = date;
        }
    }

    private async void OnDayPickerClicked(object sender, EventArgs e)
    {
        var viewModel = BindingContext as CustomDateViewModel;
        if (viewModel == null) return;

        var result = await Shell.Current.DisplayPromptAsync(
            "Select Day",
            "Enter day (DD/MM/YYYY)",
            placeholder: "15/02/2024",
            keyboard: Keyboard.Numeric);

        if (!string.IsNullOrEmpty(result) && DateTime.TryParseExact(result, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            viewModel.SelectedDay = date;
        }
    }
}
