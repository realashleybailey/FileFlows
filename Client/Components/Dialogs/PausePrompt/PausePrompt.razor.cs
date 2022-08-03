using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A prompt for the user to select how long to pause execution for
/// </summary>
public partial class PausePrompt : ComponentBase, IDisposable
{
    private string lblOk, lblCancel;
    private string Message, Title;
    TaskCompletionSource<int> ShowTask;

    private Dictionary<int, string> Durations = new()
    {
        { int.MaxValue, "Indefinite" },
        { 5, "5 minutes" },
        { 30, "30 minutes" },
        { 60, "1 hour" },
        { 120, "2 hours" },
        { 180, "3 hours" },
        { 240, "4 hours" },
        { 300, "5 hours" },
        { 360, "6 hours" },
        { 720, "12 hours" },
    };


    private static PausePrompt Instance { get; set; }

    private bool Visible { get; set; }

    private int Value { get; set; }

    private string Uid = System.Guid.NewGuid().ToString();

    private bool Focus;

    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblOk = Translater.Instant("Labels.Ok");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.Title = Translater.Instant("Dialogs.PauseDialog.Title");
        this.Message = Translater.Instant("Dialogs.PauseDialog.Message");
        Instance = this;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }
    
    /// <summary>
    /// Shows the pause prompt
    /// </summary>
    /// <returns>the selected duration in minutes to pause for</returns>

    public static Task<int> Show()
    {
        if (Instance == null)
            return Task.FromResult<int>(0);

        return Instance.ShowInstance();
    }

    private Task<int> ShowInstance()
    {
        this.Value = int.MaxValue;
        this.Visible = true;
        this.Focus = true;
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource<int>();
        return Instance.ShowTask.Task;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Focus)
        {
            Focus = false;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        }
    }


    private async void Accept()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult(Value);
        await Task.CompletedTask;
    }

    private async void Cancel()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult(0);
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}
