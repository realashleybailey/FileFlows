using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dashboard;

public partial class BoxPlot:ComponentBase
{
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
    [Parameter] public string Icon { get; set; }

    
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    [Parameter] public IEnumerable<BoxPlotData> Data { get; set; }

}