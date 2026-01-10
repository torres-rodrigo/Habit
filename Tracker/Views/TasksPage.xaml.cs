using Tracker.ViewModels;

namespace Tracker.Views;

public partial class TasksPage : ContentPage
{
    private readonly TaskViewModel _viewModel;

    public TasksPage(TaskViewModel viewModel)
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
