using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Confirm dialog that prompts the user for confirmation 
/// </summary>
public partial class Confirm : ComponentBase
{
    private string lblYes, lblNo;
    private string Message, Title, SwitchMessage;
    TaskCompletionSource<bool> ShowTask;
    TaskCompletionSource<(bool, bool)> ShowSwitchTask;
    private bool ShowSwitch;
    private bool SwitchState;

    private static Confirm Instance { get; set; }

    private bool Visible { get; set; }

    protected override void OnInitialized()
    {
        this.lblYes = Translater.Instant("Labels.Yes");
        this.lblNo = Translater.Instant("Labels.No");
        Instance = this;
    }

    /// <summary>
    /// Shows a confirm message
    /// </summary>
    /// <param name="title">the title of the confirm message</param>
    /// <param name="message">the message of the confirm message</param>
    /// <returns>the task to await for the confirm result</returns>
    public static Task<bool> Show(string title, string message)
    {
        if (Instance == null)
            return Task.FromResult(false);

        return Instance.ShowInstance(title, message);
    }
    
    /// <summary>
    /// Shows a confirm message
    /// </summary>
    /// <param name="title">the title of the confirm message</param>
    /// <param name="message">the message of the confirm message</param>
    /// <param name="switchMessage">message to show with an extra switch</param>
    /// <param name="switchState">the switch state</param>
    /// <returns>the task to await for the confirm result</returns>
    public static Task<(bool Confirmed, bool SwitchState)> Show(string title, string message, string switchMessage, bool switchState = false)
    {
        if (Instance == null)
            return Task.FromResult((false, false));

        return Instance.ShowInstance(title, message, switchMessage, switchState);
    }

    private Task<bool> ShowInstance(string title, string message)
    {
        this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Confirm");
        this.Message = Translater.TranslateIfNeeded(message ?? "");
        this.ShowSwitch = false;
        this.Visible = true;
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource<bool>();
        return Instance.ShowTask.Task;
    }
    private Task<(bool, bool)> ShowInstance(string title, string message, string switchMessage, bool switchState)
    {
        this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Confirm");
        this.Message = Translater.TranslateIfNeeded(message ?? "");
        this.SwitchMessage = Translater.TranslateIfNeeded(switchMessage ?? "");
        this.ShowSwitch = true;
        this.SwitchState = switchState;
        this.Visible = true;
        this.StateHasChanged();

        Instance.ShowSwitchTask  = new TaskCompletionSource<(bool, bool)>();
        return Instance.ShowSwitchTask.Task;
    }

    private async void Yes()
    {
        this.Visible = false;
        if (ShowSwitch)
        {
            Instance.ShowSwitchTask.TrySetResult((true, SwitchState));
            await Task.CompletedTask;
        }
        else
        {
            Instance.ShowTask.TrySetResult(true);
            await Task.CompletedTask;
        }
    }

    private async void No()
    {
        this.Visible = false;
        if (ShowSwitch)
        {
            Instance.ShowSwitchTask.TrySetResult((false, SwitchState));
            await Task.CompletedTask;
        }
        else
        {
            Instance.ShowTask.TrySetResult(false);
            await Task.CompletedTask;
        }
    }
}