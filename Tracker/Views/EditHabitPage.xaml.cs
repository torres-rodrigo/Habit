using Tracker.ViewModels;

namespace Tracker.Views;

public partial class EditHabitPage : ContentPage
{
    public EditHabitPage(EditHabitViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
