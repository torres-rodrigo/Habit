using System;
using System.Threading.Tasks;

namespace Tracker.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Schedule a recurring notification for a habit
        /// </summary>
        Task ScheduleHabitReminderAsync(Guid habitId, string title, string description, TimeSpan reminderTime, DayOfWeek[] trackingDays);

        /// <summary>
        /// Cancel all notifications for a specific habit
        /// </summary>
        Task CancelHabitReminderAsync(Guid habitId);

        /// <summary>
        /// Check if notification permissions are granted
        /// </summary>
        Task<bool> RequestNotificationPermissionsAsync();
    }
}
