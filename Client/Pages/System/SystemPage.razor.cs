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

    private ApexChart<SystemValue<float>> ChartCpuUsage, ChartCpuUsageBottom;
    
    ApexChartOptions<SystemValue<float>> cpuChartOptions;
    ApexChartOptions<SystemValue<float>> cpuChartOptionsBottom;

    protected override void OnInitialized()
    {
        timerTask = TimerAsync();
        const string mainChartId = "mainChart";
        cpuChartOptions = new ()
        {
            Title = new()
            {
                Text = null
            },
            Xaxis = new()
            {
              Labels  = new ()
              {
                  Show = false
              }
            },
            Yaxis = new()
            {
                new ()
                {
                    Labels = new ()
                    {
                        Show = false
                    }
                } 
            },
            Stroke = new ()
            {
              Curve = Curve.Smooth
            },
            Theme = new()
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette2
            }
        };
        cpuChartOptions.Chart.Id = mainChartId;
        cpuChartOptions.Chart.Toolbar = new Toolbar { AutoSelected = AutoSelected.Pan, Show = false };

        cpuChartOptionsBottom = new()
        {
            Title = new()
            {
                Text = null
            },
            Xaxis = new()
            {
                Labels  = new ()
                {
                    Show = false
                }
            },
            Yaxis = new()
            {
                new ()
                {
                    Labels = new ()
                    {
                        Show = false
                    }
                } 
            },
            Theme = new()
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette2
            }
        };
        //var selectionStart = data.Min(e => e.Date).AddDays(30);
        
        cpuChartOptionsBottom.Chart.Toolbar = new Toolbar { Show = false };
        cpuChartOptionsBottom.Xaxis = new XAxis { TickPlacement = TickPlacement.On };
        cpuChartOptionsBottom.Chart.Brush = new ApexCharts.Brush { Enabled = true, Target = mainChartId };
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
            this.Data = sysInfoResult.Data;
            await ChartCpuUsage.AppendDataAsync(sysInfoResult.Data.CpuUsage);
            await ChartCpuUsageBottom.AppendDataAsync(sysInfoResult.Data.CpuUsage);
        }

        if (this.Data != null)
        {
            var min = this.Data.CpuUsage.Min(e => e.Time);
            var max = this.Data.CpuUsage.Max(e => e.Time);
            cpuChartOptionsBottom.Chart.Selection = new Selection
            {
                Enabled = true,
                Xaxis = new SelectionXaxis
                {
                    Min = min.ToUnixTimeMilliseconds(),
                    Max = max.ToUnixTimeMilliseconds()
                }
            };
        }
    }

    public void Dispose()
    {
        timerTask?.Dispose();
        timer?.Dispose();
    }
}