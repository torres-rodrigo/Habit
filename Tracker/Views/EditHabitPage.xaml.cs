using Tracker.ViewModels;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace Tracker.Views;

public partial class EditHabitPage : ContentPage
{
    public EditHabitPage(EditHabitViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Configure focusable controls to not receive focus from Windows click bubbling
        HabitNameEntry.HandlerChanged += OnEntryHandlerChanged;
        DescriptionEditor.HandlerChanged += OnEditorHandlerChanged;
    }

    private void OnEntryHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (HabitNameEntry.Handler?.PlatformView is TextBox textBox)
        {
            textBox.IsTabStop = false;
        }
#endif
    }

    private void OnEditorHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (DescriptionEditor.Handler?.PlatformView is TextBox textBox)
        {
            textBox.IsTabStop = false;
        }
#endif
    }

    // Called when user taps directly on the Entry - manually give it focus
    private void OnHabitNameEntryTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if (HabitNameEntry.Handler?.PlatformView is TextBox textBox)
        {
            textBox.IsTabStop = true;
        }
#endif
        HabitNameEntry.Focus();
    }

    // Called when user taps directly on the Editor - manually give it focus
    private void OnDescriptionEditorTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if (DescriptionEditor.Handler?.PlatformView is TextBox textBox)
        {
            textBox.IsTabStop = true;
        }
#endif
        DescriptionEditor.Focus();
    }

    // Called when calendar is tapped - ensure text controls lose focus
    private void OnCalendarTapped(object? sender, TappedEventArgs e)
    {
        HabitNameEntry.Unfocus();
        DescriptionEditor.Unfocus();
#if WINDOWS
        if (HabitNameEntry.Handler?.PlatformView is TextBox entryTextBox)
        {
            entryTextBox.IsTabStop = false;
        }
        if (DescriptionEditor.Handler?.PlatformView is TextBox editorTextBox)
        {
            editorTextBox.IsTabStop = false;
        }
#endif
    }
}
