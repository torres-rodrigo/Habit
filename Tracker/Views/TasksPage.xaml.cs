using Tracker.Models;
using Tracker.ViewModels;

namespace Tracker.Views;

public partial class TasksPage : ContentPage
{
    private readonly TaskViewModel _viewModel;
    private readonly CustomDateViewModel _customDateViewModel;
    private bool _isTogglingSubTask = false;

    public TasksPage(TaskViewModel viewModel, CustomDateViewModel customDateViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _customDateViewModel = customDateViewModel;
        BindingContext = _viewModel;

        // Set the BindingContext for the custom date popup
        CustomDatePopupControl.BindingContext = customDateViewModel;

        // Manually bind the popup visibility to the ViewModel property
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TaskViewModel.ShowCustomDatePopup))
            {
                CustomDatePopupControl.IsVisible = _viewModel.ShowCustomDatePopup;

                // Reset selections when popup opens
                if (_viewModel.ShowCustomDatePopup)
                {
                    _customDateViewModel.ResetAllSelections();
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure popup is hidden on startup
        CustomDatePopupControl.IsVisible = false;

        await _viewModel.RefreshAsync();
    }

    private void OnCustomDateTextTapped(object sender, EventArgs e)
    {
        // Open the custom date popup when the display text is tapped
        _viewModel.ShowCustomDatePopup = true;
    }

    private void OnSubTaskCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        // Prevent re-entry to avoid infinite loops
        if (_isTogglingSubTask) return;

        try
        {
            _isTogglingSubTask = true;

            if (sender is CheckBox checkBox && checkBox.BindingContext is SubTask subTask)
            {
                // Find the parent TodoTask by walking up the visual tree
                Element? parent = checkBox.Parent;
                while (parent != null)
                {
                    if (parent.BindingContext is TodoTask task)
                    {
                        // Call the ViewModel's command
                        if (_viewModel.ToggleSubTaskCompletionCommand.CanExecute((task.Id, subTask.Id)))
                        {
                            _viewModel.ToggleSubTaskCompletionCommand.Execute((task.Id, subTask.Id));
                        }
                        break;
                    }
                    parent = parent.Parent;
                }
            }
        }
        finally
        {
            _isTogglingSubTask = false;
        }
    }
}
