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
public partial class SystemPage:ComponentBase
{
    private string lblCpuUsage, lblMemoryUsage, lblTempStorage, lblLibraryProcessingTimes, lblProcessingHeatMap;

    private string CpuUrl, MemoryUrl, TempStorageUrl, LibraryProcessingTimeUrl, ProcessingHeatMapUrl;

    protected override async Task OnInitializedAsync()
    {
        this.CpuUrl = "/api/system/history-data/cpu";
        this.MemoryUrl = "/api/system/history-data/memory";
        this.TempStorageUrl = "/api/system/history-data/temp-storage";
        this.LibraryProcessingTimeUrl  = "/api/system/history-data/library-processing-time";
        this.ProcessingHeatMapUrl  = "/api/system/history-data/processing-heatmap";
#if (DEBUG)
        this.CpuUrl = "http://localhost:6868" + this.CpuUrl;
        this.MemoryUrl = "http://localhost:6868" + this.MemoryUrl;
        this.TempStorageUrl = "http://localhost:6868" + this.TempStorageUrl;
        this.LibraryProcessingTimeUrl = "http://localhost:6868" + this.LibraryProcessingTimeUrl;
        this.ProcessingHeatMapUrl = "http://localhost:6868" + this.ProcessingHeatMapUrl;
#endif
        this.lblCpuUsage = Translater.Instant("Pages.System.Labels.CpuUsage");
        this.lblMemoryUsage = Translater.Instant("Pages.System.Labels.MemoryUsage");
        this.lblTempStorage = Translater.Instant("Pages.System.Labels.TempStorage");
        this.lblLibraryProcessingTimes = Translater.Instant("Pages.System.Labels.LibraryProcessingTimes");
        this.lblProcessingHeatMap = Translater.Instant("Pages.System.Labels.ProcessingHeatMap");
        //await Refresh();
        //timerTask = TimerAsync();
    }
}