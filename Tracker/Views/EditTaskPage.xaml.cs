using Tracker.ViewModels;

namespace Tracker.Views;

public partial class EditTaskPage : ContentPage
{
    public EditTaskPage(EditTaskViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
