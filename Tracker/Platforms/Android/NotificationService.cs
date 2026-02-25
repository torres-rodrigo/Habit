using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Tracker.Services;

namespace Tracker.Platforms.Android
{
    public class NotificationService : INotificationService
    {
        private const string ChannelId = "habit_reminders";
        private const string ChannelName = "Habit Reminders";

        public NotificationService()
        {
            CreateNotificationChannel();
        }

        public async Task ScheduleHabitReminderAsync(Guid habitId, string title, string description, TimeSpan reminderTime, DayOfWeek[] trackingDays)
        {
            await Task.Run(() =>
            {
                var context = Platform.CurrentActivity ?? Application.Context;

                // Cancel existing notifications first
                CancelHabitReminderAsync(habitId).Wait();

                // Truncate description to first 2 lines (approximately 100 chars)
                var shortDescription = description?.Length > 100
                    ? description.Substring(0, 100) + "..."
                    : description ?? "";

                var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);

                // Schedule notification for each tracking day
                for (int i = 0; i < trackingDays.Length; i++)
                {
                    var day = trackingDays[i];
                    var intent = new Intent(context, typeof(NotificationReceiver));
                    intent.PutExtra("habitId", habitId.ToString());
                    intent.PutExtra("title", title);
                    intent.PutExtra("description", shortDescription);

                    var requestCode = GetRequestCode(habitId, day);
                    var pendingIntent = PendingIntent.GetBroadcast(
                        context,
                        requestCode,
                        intent,
                        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                    var triggerTime = GetNextOccurrence(day, reminderTime);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                    {
                        if (alarmManager.CanScheduleExactAlarms())
                        {
                            alarmManager.SetRepeating(
                                AlarmType.RtcWakeup,
                                triggerTime,
                                AlarmInterval.Day * 7, // Weekly repeat
                                pendingIntent);
                        }
                    }
                    else
                    {
                        alarmManager.SetRepeating(
                            AlarmType.RtcWakeup,
                            triggerTime,
                            AlarmInterval.Day * 7,
                            pendingIntent);
                    }
                }
            });
        }

        public async Task CancelHabitReminderAsync(Guid habitId)
        {
            await Task.Run(() =>
            {
                var context = Platform.CurrentActivity ?? Application.Context;
                var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);

                // Cancel all possible day notifications for this habit
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    var intent = new Intent(context, typeof(NotificationReceiver));
                    var requestCode = GetRequestCode(habitId, day);
                    var pendingIntent = PendingIntent.GetBroadcast(
                        context,
                        requestCode,
                        intent,
                        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                }
            });
        }

        public async Task<bool> RequestNotificationPermissionsAsync()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                return status == PermissionStatus.Granted;
            }
            return true;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
                {
                    Description = "Reminders for your daily habits"
                };

                var notificationManager = (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private long GetNextOccurrence(DayOfWeek targetDay, TimeSpan time)
        {
            var now = DateTime.Now;
            var today = now.DayOfWeek;

            int daysUntilTarget = ((int)targetDay - (int)today + 7) % 7;
            if (daysUntilTarget == 0 && now.TimeOfDay > time)
            {
                daysUntilTarget = 7;
            }

            var targetDate = now.Date.AddDays(daysUntilTarget).Add(time);
            return (long)(targetDate - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private int GetRequestCode(Guid habitId, DayOfWeek day)
        {
            // Generate unique request code from habitId and day
            return (habitId.GetHashCode() & 0xFFFFFF) | ((int)day << 24);
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class NotificationReceiver : BroadcastReceiver
    {
        private const string ChannelId = "habit_reminders";

        public override void OnReceive(Context context, Intent intent)
        {
            var habitId = intent.GetStringExtra("habitId");
            var title = intent.GetStringExtra("title");
            var description = intent.GetStringExtra("description");

            var notificationIntent = new Intent(context, typeof(MainActivity));
            notificationIntent.PutExtra("openPage", "habits");
            notificationIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                0,
                notificationIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = new NotificationCompat.Builder(context, ChannelId)
                .SetContentTitle(title)
                .SetContentText(description)
                .SetSmallIcon(Resource.Drawable.appicon)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetPriority(NotificationCompat.PriorityHigh)
                .Build();

            var notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Notify(habitId.GetHashCode(), notification);
        }
    }
}
