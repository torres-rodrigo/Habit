namespace Tracker.Services
{
    public interface IDialogService
    {
        Task DisplayAlertAsync(string title, string message, string accept);
        Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    }

    public class ShellDialogService : IDialogService
    {
        public Task DisplayAlertAsync(string title, string message, string accept)
            => Shell.Current.DisplayAlert(title, message, accept);

        public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
            => Shell.Current.DisplayAlert(title, message, accept, cancel);
    }
}
