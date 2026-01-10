using Microsoft.Extensions.Logging;
using Tracker.Services;
using Tracker.ViewModels;
using Tracker.Views;

namespace Tracker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Services
		builder.Services.AddSingleton<IDataService, DataService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();

		// Register ViewModels
		builder.Services.AddTransient<StatisticsViewModel>();
		builder.Services.AddTransient<HabitViewModel>();
		builder.Services.AddTransient<TaskViewModel>();
		builder.Services.AddTransient<EditHabitViewModel>();
		builder.Services.AddTransient<EditTaskViewModel>();
		builder.Services.AddTransient<SelectItemTypeViewModel>();

		// Register Views
		builder.Services.AddTransient<StatisticsPage>();
		builder.Services.AddTransient<HabitsPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<EditHabitPage>();
		builder.Services.AddTransient<EditTaskPage>();
		builder.Services.AddTransient<SelectItemTypePage>();

		return builder.Build();
	}
}
