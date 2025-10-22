using System.Threading.Tasks;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace PythonIpcTool.Services;

/// <summary>
/// A design-time safe implementation of IDialogCoordinator that provides default
/// return values for all interface methods, preventing exceptions in the XAML designer.
/// </summary>
public class DesignDialogCoordinator : IDialogCoordinator
{
    // --- Input and Message Dialogs ---

    public Task<string?> ShowInputAsync(object context, string title, string message, MetroDialogSettings? settings = null)
    {
        // Simulate user canceling the dialog
        return Task.FromResult<string?>(null);
    }

    public Task<MessageDialogResult> ShowMessageAsync(object context, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings? settings = null)
    {
        // Simulate user clicking the default button
        return Task.FromResult(MessageDialogResult.Affirmative);
    }

    // --- Login Dialog (This was the one causing the error) ---

    public Task<LoginDialogData?> ShowLoginAsync(object context, string title, string message, LoginDialogSettings? settings = null)
    {
        // Simulate user canceling the login
        return Task.FromResult<LoginDialogData?>(null);

    }

    // --- Progress Dialog ---

    public Task<ProgressDialogController> ShowProgressAsync(object context, string title, string message, bool isCancelable = false, MetroDialogSettings? settings = null)
    {
        // The constructor for ProgressDialogController is internal in recent versions of MahApps.Metro.
        // We cannot create a new instance here.
        // Returning null is the safest and only viable option for a design-time mock.
        // The null-forgiving operator (!) is used to suppress compiler warnings about returning null.
        return Task.FromResult<ProgressDialogController>(null!);
    }

    // --- Custom Dialogs ---

    public Task ShowMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings? settings = null)
    {
        return Task.CompletedTask;
    }

    public Task HideMetroDialogAsync(object context, BaseMetroDialog dialog, MetroDialogSettings? settings = null)
    {
        return Task.CompletedTask;
    }

    public Task<TDialog> GetCurrentDialogAsync<TDialog>(object context) where TDialog : BaseMetroDialog
    {
        // Use default keyword to return the default value for TDialog (which is null for reference types)
        return Task.FromResult(default(TDialog)!);
    }

    public string ShowModalInputExternal(object context, string title, string message, MetroDialogSettings settings = null)
    {
        throw new NotImplementedException();
    }

    public LoginDialogData ShowModalLoginExternal(object context, string title, string message, LoginDialogSettings settings = null)
    {
        throw new NotImplementedException();
    }

    public MessageDialogResult ShowModalMessageExternal(object context, string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
    {
        throw new NotImplementedException();
    }
}