using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using ApexCharts;
using FileFlows.Client.Components.Dashboard;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page that displays system information
/// </summary>
public partial class SystemPage:ComponentBase, IDisposable
{
    private Task? timerTask;
    private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
    private readonly CancellationTokenSource cancellationToken = new();

    private SystemInfoData? Data;

    private SystemValueLineChartApex<float> chartCpuUsage;
    private SystemValueLineChart<float> chartMemoryUsage;
    private SystemValueLineChart<long> chartTempStorage;

    private string lblCpuUsage, lblMemoryUsage, lblTempStorage, lblLibraryProcessingTimes;

    private string CpuUrl;

    protected override async Task OnInitializedAsync()
    {
        this.CpuUrl = "/api/system/history-data/cpu";
#if (DEBUG)
        this.CpuUrl = "http://localhost:6868" + this.CpuUrl;
#endif
        this.lblCpuUsage = Translater.Instant("Pages.System.Labels.CpuUsage");
        this.lblMemoryUsage = Translater.Instant("Pages.System.Labels.MemoryUsage");
        this.lblTempStorage = Translater.Instant("Pages.System.Labels.TempStorage");
        this.lblLibraryProcessingTimes = Translater.Instant("Pages.System.Labels.LibraryProcessingTimes");
        //await Refresh();
        //timerTask = TimerAsync();
    }

    private async Task TimerAsync()
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken.Token))
            {
                await Refresh();
            }
        }
        catch (Exception)
        {
        }
    }

    private async Task Refresh()
    {
        var sysInfoResult = await HttpHelper.Get<SystemInfoData>("/api/system/history-data" + (Data == null ? "" : "?since=" + Data.SystemDateTime));
        if (sysInfoResult.Success == false)
            return;
        this.Data = sysInfoResult.Data;
        if(chartCpuUsage != null)
            await chartCpuUsage.AppendData(sysInfoResult.Data.CpuUsage);
        if (chartMemoryUsage != null)
            await chartMemoryUsage.AppendData(sysInfoResult.Data.MemoryUsage);
        if (chartTempStorage != null)
            await chartTempStorage.AppendData(sysInfoResult.Data.TempStorageUsage);

    }

    public void Dispose()
    {
        if (timerTask is null)
            return;
        
        cancellationToken.Cancel();
        Task.Run(async () =>
        {
            await timerTask;
            cancellationToken.Dispose();
            timerTask?.Dispose();
            timer?.Dispose();
        });
    }
}