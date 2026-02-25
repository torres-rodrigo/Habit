using System;
using System.Linq;
using System.Threading.Tasks;
using Tracker.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Tracker.Platforms.Windows
{
    public class NotificationService : INotificationService
    {
        private ToastNotifier? _notifier;

        private ToastNotifier? GetNotifier()
        {
            try
            {
                if (_notifier == null)
                {
                    _notifier = ToastNotificationManager.CreateToastNotifier();
                }
                return _notifier;
            }
            catch
            {
                // App doesn't have package identity or notifications aren't available
                return null;
            }
        }

        public async Task ScheduleHabitReminderAsync(Guid habitId, string title, string description, TimeSpan reminderTime, DayOfWeek[] trackingDays)
        {
            var notifier = GetNotifier();
            if (notifier == null)
                return; // Notifications not available

            await Task.Run(() =>
            {
                // Cancel existing notifications first
                CancelHabitReminderAsync(habitId).Wait();

                // Truncate description to first 2 lines (approximately 100 chars)
                var shortDescription = description?.Length > 100
                    ? description.Substring(0, 100) + "..."
                    : description ?? "";

                // Schedule notification for each tracking day
                foreach (var day in trackingDays)
                {
                    var xml = $@"
                        <toast launch='habit:{habitId}'>
                            <visual>
                                <binding template='ToastGeneric'>
                                    <text>{System.Security.SecurityElement.Escape(title)}</text>
                                    <text>{System.Security.SecurityElement.Escape(shortDescription)}</text>
                                </binding>
                            </visual>
                            <actions>
                                <action content='Open' arguments='habit:{habitId}' activationType='foreground'/>
                                <action content='Dismiss' arguments='dismiss' activationType='background'/>
                            </actions>
                        </toast>";

                    var doc = new XmlDocument();
                    doc.LoadXml(xml);

                    var scheduledTime = GetNextOccurrence(day, reminderTime);
                    var scheduledToast = new ScheduledToastNotification(doc, scheduledTime)
                    {
                        Id = $"habit_{habitId}_{day}",
                        Tag = habitId.ToString()
                    };

                    notifier.AddToSchedule(scheduledToast);
                }
            });
        }

        public async Task CancelHabitReminderAsync(Guid habitId)
        {
            var notifier = GetNotifier();
            if (notifier == null)
                return; // Notifications not available

            await Task.Run(() =>
            {
                var scheduled = notifier.GetScheduledToastNotifications();
                foreach (var notification in scheduled.Where(n => n.Tag == habitId.ToString()))
                {
                    notifier.RemoveFromSchedule(notification);
                }
            });
        }

        public Task<bool> RequestNotificationPermissionsAsync()
        {
            // Windows doesn't require runtime permission request
            return Task.FromResult(true);
        }

        private DateTimeOffset GetNextOccurrence(DayOfWeek targetDay, TimeSpan time)
        {
            var now = DateTime.Now;
            var today = now.DayOfWeek;

            int daysUntilTarget = ((int)targetDay - (int)today + 7) % 7;
            if (daysUntilTarget == 0 && now.TimeOfDay > time)
            {
                daysUntilTarget = 7; // Schedule for next week if time already passed today
            }

            var targetDate = now.Date.AddDays(daysUntilTarget).Add(time);
            return new DateTimeOffset(targetDate);
        }
    }
}
