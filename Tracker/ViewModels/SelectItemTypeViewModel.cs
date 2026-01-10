using System.Windows.Input;

namespace Tracker.ViewModels;

public class SelectItemTypeViewModel : BaseViewModel
{
    public ICommand NavigateToHabitCommand { get; }
    public ICommand NavigateToTaskCommand { get; }
    public ICommand CancelCommand { get; }

    public SelectItemTypeViewModel()
    {
        NavigateToHabitCommand = new Command(async () => await NavigateToHabit());
        NavigateToTaskCommand = new Command(async () => await NavigateToTask());
        CancelCommand = new Command(async () => await Cancel());
    }

    private async Task NavigateToHabit()
    {
        await Shell.Current.GoToAsync("../habits/edithabit");
    }

    private async Task NavigateToTask()
    {
        await Shell.Current.GoToAsync("../tasks/edittask");
    }

    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}
