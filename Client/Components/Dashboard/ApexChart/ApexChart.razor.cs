using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

public partial class ApexChart:ComponentBase, IDisposable
{
    /// <summary>
    /// The javascript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Gets or sets the chart type
    /// </summary>
    [Parameter] public ApexChartType ChartType { get; set; }

    /// <summary>
    /// Gets or sets the width of the chart
    /// </summary>
    [Parameter] public int Width { get; set; } = 1;
    /// <summary>
    /// Gets or sets the height of the chart
    /// </summary>
    [Parameter] public int Height { get; set; } = 1;

    private string _Title = string.Empty;
    /// <summary>
    /// Gets or sets the title 
    /// </summary>
    [Parameter] public string Title
    {
        get => _Title;
        set
        {
            _Title = Translater.TranslateIfNeeded(value);
        }
    }

    /// <summary>
    /// Gets or sets the icon to show
    /// </summary>
    [Parameter]
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the URL to retrieve the data
    /// </summary>
    [Parameter] public string Url { get; set; }
        
    /// <summary>
    /// UID of the chart, this is used to load/save the charts position
    /// </summary>
    private string Uid;
    
    private IJSObjectReference jsCharts;

    protected override void OnInitialized()
    {
        this.Uid = this.Title.Dehumanize();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            jsCharts = await jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./scripts/Charts/FFChart.js");
            await jsCharts.InvokeVoidAsync($"newChart",  ChartType.ToString(), Uid, new
            {
                this.Url,
                this.Title
            }); 
        }
    }

    public void Dispose()
    {
        _ = jsCharts.InvokeVoidAsync("dispose", Uid);
    }
}

/// <summary>
/// Available chart types
/// </summary>
public enum ApexChartType
{
    /// <summary>
    /// Box plot 
    /// </summary>
    BoxPlot,
    /// <summary>
    /// Heat map
    /// </summary>
    HeatMap,
    /// <summary>
    /// Pie chart
    /// </summary>
    PieChart,
    /// <summary>
    /// Tree map
    /// </summary>
    TreeMap,
    /// <summary>
    /// Time series
    /// </summary>
    TimeSeries
}