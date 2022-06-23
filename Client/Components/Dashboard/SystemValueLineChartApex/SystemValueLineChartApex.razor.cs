using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;


namespace FileFlows.Client.Components.Dashboard;

public partial class SystemValueLineChartApex<TItem>:ComponentBase, IDisposable
{
    [Inject] public IJSRuntime jsRuntime { get; set; }
    private string _Title = string.Empty;
    
    // green:
    private string colorGreen = "#00ff00";
    // blue: 
    private string colorBlue = "#00BAEC";
    
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
    [Parameter] public string Icon { get; set; }

    /// <summary>
    /// Gets or sets if this data is size data, eg KB, MB, GB etc
    /// </summary>
    [Parameter] public bool SizeData { get; set; }

    /// <summary>
    /// Gets or sets the URL to retrieve the data
    /// </summary>
    [Parameter] public string Url { get; set; }

    private string Uid, BottomUid, TopUid;
    public IJSObjectReference jsCharts;
    
    protected override void OnInitialized()
    {
        Uid = Guid.NewGuid().ToString();
        BottomUid = Uid + "-bottom";
        TopUid = Uid + "-top";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            jsCharts = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/SystemValueLineChart.js");
            await jsCharts.InvokeVoidAsync("newSystemValueLineChart", Uid, new
            {
                this.Url,
                this.SizeData,
                this.Title
            }); 
        }
    }

    public void Dispose()
    {
        _ = jsCharts.InvokeVoidAsync("dispose", Uid);
    }
}