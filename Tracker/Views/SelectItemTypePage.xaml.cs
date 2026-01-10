using Tracker.ViewModels;

namespace Tracker.Views;

public partial class SelectItemTypePage : ContentPage
{
    public SelectItemTypePage(SelectItemTypeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
