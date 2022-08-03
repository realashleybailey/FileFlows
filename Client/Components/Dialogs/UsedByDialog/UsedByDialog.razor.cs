using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Confirm dialog that prompts the user for confirmation 
/// </summary>
public partial class UsedByDialog : ComponentBase, IDisposable
{
    private string lblTitle, lblClose, lblName, lblType;
    TaskCompletionSource ShowTask;
    private List<ObjectReference> UsedBy;

    private static UsedByDialog Instance { get; set; }

    private bool Visible { get; set; }

    protected override void OnInitialized()
    {
        this.lblClose = Translater.Instant("Labels.Close");
        this.lblTitle = Translater.TranslateIfNeeded("Labels.UsedBy");
        this.lblName = Translater.TranslateIfNeeded("Labels.Name");
        this.lblType = Translater.TranslateIfNeeded("Labels.Type");
        Instance = this;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Close();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Shows a used by dialog
    /// </summary>
    /// <param name="usedBy">a list of items this is used by</param>
    /// <returns>the task to await for the dialog to close</returns>
    public static Task Show(List<ObjectReference> usedBy)
    {
        if (Instance == null)
            return Task.FromResult(false);

        return Instance.ShowInstance(usedBy);
    }

    private Task ShowInstance(List<ObjectReference> usedBy)
    {
        this.UsedBy = usedBy;
        this.Visible = true;
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource();
        return Instance.ShowTask.Task;
    }

    private async void Close()
    {
        this.Visible = false;
        Instance.ShowTask.SetResult();
        await Task.CompletedTask;
    }

    private string GetTypeName(string type)
    {
        type = type.Substring(type.LastIndexOf(".") + 1);
        return Translater.Instant($"Pages.{type}.Title");
    }

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}