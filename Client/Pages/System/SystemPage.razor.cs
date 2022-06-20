using System.Threading;
using System.Timers;
using ApexCharts;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page that displays system information
/// </summary>
public partial class SystemPage:ComponentBase, IDisposable
{
    private Task? timerTask;
    private readonly PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
    private readonly CancellationToken cancellationToken = new();

    private SystemInfoData? Data;

    private ApexChart<SystemValue<float>> ChartCpuUsage;
    
    ApexChartOptions<SystemValue<float>> ChartOptions;

    protected override void OnInitialized()
    {
        timerTask = TimerAsync();
        ChartOptions = new ()
        {
            Theme = new Theme
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette2
            }
        };
    }

    private async Task TimerAsync()
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
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
        bool initial = this.Data == null;
        if (initial)
        {
            var sysInfoResult = await HttpHelper.Get<SystemInfoData>("/api/system/history-data");
            if (sysInfoResult.Success == false)
                return;
            this.Data = sysInfoResult.Data;
            this.StateHasChanged();
        }
        else
        {
            var sysInfoResult = await HttpHelper.Get<SystemInfoData>("/api/system/history-data?since=" + Data.SystemDateTime);
            if (sysInfoResult.Success == false)
                return;
            await ChartCpuUsage.AppendDataAsync(sysInfoResult.Data.CpuUsage);
        }
    }

    public void Dispose()
    {
        timerTask?.Dispose();
        timer?.Dispose();
    }
}