using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page that displays system information
/// </summary>
public partial class SystemPage:ComponentBase
{
    private string lblCpuUsage, lblMemoryUsage, lblTempStorage, lblLogStorage, lblLibraryProcessingTimes, lblProcessingHeatMap, lblCodec,
        lblVideoContainer, lblVideoResolution;

    private string CpuUrl, MemoryUrl, TempStorageUrl, LogStorageUrl, LibraryProcessingTimeUrl, ProcessingHeatMapUrl, VideoContainerUrl,
        CodecUrl, VideoResolutionUrl;

    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override async Task OnInitializedAsync()
    {
        this.CpuUrl = "/api/system/history-data/cpu";
        this.MemoryUrl = "/api/system/history-data/memory";
        this.TempStorageUrl = "/api/system/history-data/temp-storage";
        this.LogStorageUrl = "/api/system/history-data/log-storage";
        this.LibraryProcessingTimeUrl  = "/api/system/history-data/library-processing-time";
        this.ProcessingHeatMapUrl  = "/api/system/history-data/processing-heatmap";
        this.CodecUrl = "/api/statistics/by-name/CODEC";
        this.VideoContainerUrl = "/api/statistics/by-name/VIDEO_CONTAINER";
        this.VideoResolutionUrl = "/api/statistics/by-name/VIDEO_RESOLUTION";
#if (DEBUG)
        this.CpuUrl = "http://localhost:6868" + this.CpuUrl;
        this.MemoryUrl = "http://localhost:6868" + this.MemoryUrl;
        this.TempStorageUrl = "http://localhost:6868" + this.TempStorageUrl;
        this.LogStorageUrl = "http://localhost:6868" + this.LogStorageUrl;
        this.LibraryProcessingTimeUrl = "http://localhost:6868" + this.LibraryProcessingTimeUrl;
        this.ProcessingHeatMapUrl = "http://localhost:6868" + this.ProcessingHeatMapUrl;
        this.CodecUrl = "http://localhost:6868" + this.CodecUrl;
        this.VideoContainerUrl = "http://localhost:6868" + this.VideoContainerUrl;
        this.VideoResolutionUrl = "http://localhost:6868" + this.VideoResolutionUrl;
#endif
        this.lblCpuUsage = Translater.Instant("Pages.System.Labels.CpuUsage");
        this.lblMemoryUsage = Translater.Instant("Pages.System.Labels.MemoryUsage");
        this.lblTempStorage = Translater.Instant("Pages.System.Labels.TempStorage");
        this.lblLogStorage = Translater.Instant("Pages.System.Labels.LogStorage");
        this.lblLibraryProcessingTimes = Translater.Instant("Pages.System.Labels.LibraryProcessingTimes");
        this.lblProcessingHeatMap = Translater.Instant("Pages.System.Labels.ProcessingHeatMap");
        this.lblCodec = Translater.Instant("Pages.System.Labels.Codec");
        this.lblVideoContainer = Translater.Instant("Pages.System.Labels.VideoContainer");
        this.lblVideoResolution = Translater.Instant("Pages.System.Labels.VideoResolution");
    }

    // protected override async Task OnAfterRenderAsync(bool firstRender)
    // {
    //     await jsRuntime.InvokeVoidAsync("eval", "(new Muuri('.dashboard'))");
    // }
}