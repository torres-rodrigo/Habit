using Tracker.Models;
using Tracker.ViewModels;

namespace Tracker.Views;

public partial class TasksPage : ContentPage
{
    private readonly TaskViewModel _viewModel;
    private bool _isTogglingSubTask = false;

    public TasksPage(TaskViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
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
