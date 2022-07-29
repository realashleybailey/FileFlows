using System.ComponentModel.DataAnnotations;
using System.Timers;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Shared.Json;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components.Dashboard;

public partial class PauseResume: IDisposable
{
    private SystemInfo SystemInfo = new SystemInfo();
    private bool Refreshing = false;
    
    private string lblPauseLabel;
    private BackgroundTask bkgTask;
    private TimeSpan TimeDiff;
    private DateTime LastUpdated = DateTime.MinValue;
    private string lblPause, lblResume, lblPaused;

    protected override async Task OnInitializedAsync()
    {
        lblPause = Translater.Instant("Labels.Pause");
        lblPaused = Translater.Instant("Labels.Paused");
        lblResume = Translater.Instant("Labels.Resume");
        await this.Refresh();
        bkgTask = new BackgroundTask(TimeSpan.FromMilliseconds(1_000), () => _ = DoWork());
        bkgTask.Start();
    }

    private async Task DoWork()
    {
        if (LastUpdated < DateTime.Now.AddSeconds(-5))
        {
            await Refresh();
        }
        UpdateTime();
    }
    
    public void Dispose()
    {
        _ = bkgTask?.StopAsync();
        bkgTask = null;
    }

    private void UpdateTime()
    {
        if (SystemInfo.IsPaused == false)
        {
            lblPauseLabel = lblPause;
            return;
        }

        if (SystemInfo.PausedUntil > SystemInfo.CurrentTime.AddYears(1))
        {
            lblPauseLabel = lblPaused;
            return;
        }
        
        var pausedToLocal = SystemInfo.PausedUntil.Add(TimeDiff);
        var time = pausedToLocal.Subtract(DateTime.Now);
        lblPauseLabel = lblPaused + " (" + time.ToString(@"h\:mm\:ss") + ")";
        this.StateHasChanged();
    }
    
    async Task Refresh()
    {
        if (Refreshing)
            return;
        Refreshing = true;
        try
        {
            RequestResult<List<FlowExecutorInfo>> result = null;
            RequestResult<SystemInfo> systemInfoResult = await GetSystemInfo();
            
            if (systemInfoResult.Success)
            {
                TimeDiff = DateTime.Now - systemInfoResult.Data.CurrentTime;
                this.SystemInfo = systemInfoResult.Data;
                UpdateTime();
            }
                
        }
        catch (Exception)
        {
        }
        finally
        {
            LastUpdated = DateTime.Now;
            Refreshing = false;
        }
    }

    Task<RequestResult<SystemInfo>> GetSystemInfo() => HttpHelper.Get<SystemInfo>("/api/system/info");

    private async Task TogglePaused()
    {
        bool paused = SystemInfo.IsPaused;
        int duration = 0;
        if (paused == false)
        {
            duration = await PausePrompt.Show();
            if (duration < 1)
                return;

        }

        paused = !paused;
        await HttpHelper.Post($"/api/system/pause?duration=" + duration);
        var systemInfoResult = await GetSystemInfo();
        if (systemInfoResult.Success)
        {
            TimeDiff = DateTime.Now - systemInfoResult.Data.CurrentTime;
            SystemInfo = systemInfoResult.Data;
            this.UpdateTime();
            this.StateHasChanged();
        }
    }
}