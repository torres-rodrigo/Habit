using Tracker.Views;
using Tracker.ViewModels;

namespace Tracker.Services;

public interface INavigationService
{
    Task NavigateToAsync(string route, Dictionary<string, object>? parameters = null);
    Task GoBackAsync();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task NavigateToAsync(string route, Dictionary<string, object>? parameters = null)
    {
        Page? page = route switch
        {
            "selectitemtype" => _serviceProvider.GetRequiredService<SelectItemTypePage>(),
            "habits/edithabit" => _serviceProvider.GetRequiredService<EditHabitPage>(),
            "habits/addhabit" => _serviceProvider.GetRequiredService<EditHabitPage>(),
            "tasks/edittask" => _serviceProvider.GetRequiredService<EditTaskPage>(),
            "tasks/addtask" => _serviceProvider.GetRequiredService<EditTaskPage>(),
            _ => null
        };

        if (page != null)
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    page.BindingContext ??= new object();
                    // Parameters will be handled by the QueryProperty attributes
                }
            }
            
            var window = Application.Current?.Windows[0];
            if (window?.Page?.Navigation != null)
            {
                await window.Page.Navigation.PushModalAsync(page);
            }
        }
    }

    public async Task GoBackAsync()
    {
        var window = Application.Current?.Windows[0];
        if (window?.Page?.Navigation?.ModalStack.Count > 0)
        {
            await window.Page.Navigation.PopModalAsync();
        }
    }
}
