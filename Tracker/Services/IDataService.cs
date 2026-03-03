using Tracker.Models;

namespace Tracker.Services
{
    public interface IDataService
    {
        // Habits
        Task<List<Habit>> GetAllHabitsAsync();
        Task<Habit?> GetHabitByIdAsync(Guid id);
        Task SaveHabitAsync(Habit habit);
        Task DeleteHabitAsync(Guid id);
        Task UpdateHabitOrderAsync(List<Habit> habits);
        Task UpdateHabitDisplayOrderAsync(Guid habitId, int displayOrder);
        Task ToggleHabitCompletionAsync(Guid habitId, DateTime date, string? note = null);
        Task<bool> IsHabitCompletedOnDateAsync(Guid habitId, DateTime date);
        Task<string?> GetHabitNoteAsync(Guid habitId, DateTime date);
        Task SaveHabitNoteAsync(Guid habitId, DateTime date, string noteText);

        // Tasks
        Task<List<TodoTask>> GetAllTasksAsync();
        Task<TodoTask?> GetTaskByIdAsync(Guid id);
        Task SaveTaskAsync(TodoTask task);
        Task DeleteTaskAsync(Guid id);
        Task UpdateTaskOrderAsync(List<TodoTask> tasks);
        Task ToggleTaskCompletionAsync(Guid taskId);
        Task ToggleSubTaskCompletionAsync(Guid taskId, Guid subTaskId);

        // Statistics
        Task<HabitStatistics?> GetHabitStatisticsAsync(Guid habitId);
        Task<List<HabitStatistics>> GetAllHabitStatisticsAsync();
        Task<TaskStatistics> GetTaskStatisticsAsync();
    }
}
