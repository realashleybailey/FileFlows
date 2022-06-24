using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

public partial class BoxPlot:ComponentBase, IDisposable
{
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
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
        
    private string Uid;
    public IJSObjectReference jsCharts;

    protected override void OnInitialized()
    {
        Uid = Guid.NewGuid().ToString();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            jsCharts = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/Charts/BoxPlot.js");
            await jsCharts.InvokeVoidAsync("newBoxPlot", Uid, new
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