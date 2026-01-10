using Tracker.ViewModels;

namespace Tracker.Views;

public partial class HabitsPage : ContentPage
{
    private readonly HabitViewModel _viewModel;

    public HabitsPage(HabitViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }
}
