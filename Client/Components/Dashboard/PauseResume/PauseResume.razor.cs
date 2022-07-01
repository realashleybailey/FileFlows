using System.Timers;
using FileFlows.Client.Components.Dialogs;

namespace FileFlows.Client.Components.Dashboard;

public partial class PauseResume: IDisposable
{
    private SystemInfo SystemInfo = new SystemInfo();
    private Timer AutoRefreshTimer;
    private bool Refreshing = false;
    
    private string lblPauseLabel;

    protected override async Task OnInitializedAsync()
    {
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        await this.Refresh();
    }
    
    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }

    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _ = Refresh();
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
                this.SystemInfo = systemInfoResult.Data;
                this.lblPauseLabel =
                    Translater.Instant(systemInfoResult.Data.IsPaused ? "Labels.Resume" : "Labels.Pause");
            }
                
        }
        catch (Exception)
        {
        }
        finally
        {
            Refreshing = false;
        }
    }

    async Task<RequestResult<SystemInfo>> GetSystemInfo()
    {
#if (DEMO)
            var random = new Random(DateTime.Now.Millisecond);
            return new SystemInfo { CpuUsage = random.Next() * 100f, MemoryUsage = random.Next() * 1_000_000_000 };
#else
        return await HttpHelper.Get<SystemInfo>("/api/system/info");
#endif
    }

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
            SystemInfo = systemInfoResult.Data;
            this.lblPauseLabel = Translater.Instant(SystemInfo.IsPaused ? "Labels.Resume" : "Labels.Pause");
            this.StateHasChanged();
        }
    }
}