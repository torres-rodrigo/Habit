namespace Tracker.Services
{
    public interface INavigationService
    {
        Task GoToAsync(string route);
    }

    public class ShellNavigationService : INavigationService
    {
        public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
    }
}
