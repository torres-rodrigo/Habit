using Tracker.Views;

namespace Tracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		Routing.RegisterRoute("selectitemtype", typeof(SelectItemTypePage));
		Routing.RegisterRoute("habits/edithabit", typeof(EditHabitPage));
		Routing.RegisterRoute("tasks/edittask", typeof(EditTaskPage));

#if WINDOWS
		// Customize tab bar styling on Windows
		this.Loaded += (s, e) =>
		{
			var handler = this.Handler;
			if (handler?.PlatformView is Microsoft.UI.Xaml.Controls.NavigationView navView)
			{
				Platforms.Windows.TabBarHelper.CustomizeTabBar(navView);
			}
		};
#endif
	}
}

