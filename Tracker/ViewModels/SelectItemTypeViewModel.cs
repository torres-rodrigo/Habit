using System.Windows.Input;
using Tracker.Services;

namespace Tracker.ViewModels;

public class SelectItemTypeViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    public ICommand NavigateToHabitCommand { get; }
    public ICommand NavigateToTaskCommand { get; }
    public ICommand CancelCommand { get; }

    public SelectItemTypeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        NavigateToHabitCommand = new Command(async () => await NavigateToHabit());
        NavigateToTaskCommand = new Command(async () => await NavigateToTask());
        CancelCommand = new Command(async () => await Cancel());
    }

    private async Task NavigateToHabit()
    {
        await _navigationService.GoToAsync("../habits/edithabit");
    }

    private async Task NavigateToTask()
    {
        await _navigationService.GoToAsync("../tasks/edittask");
    }

    private async Task Cancel()
    {
        await _navigationService.GoToAsync("..");
    }
}
