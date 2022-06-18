using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Search Pane to show for a search
/// </summary>
partial class SearchPane:ComponentBase
{
    /// <summary>
    /// Gets or sets if the search pane is visible
    /// </summary>
    public bool Visible { get; set; }
    
    /// <summary>
    /// Gets or sets the child content of the search pane
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets event that is fired when the user clicks search
    /// </summary>
    [Parameter] public EventCallback Searched { get; set; }

    private string lblSearch;

    protected override void OnInitialized()
    {
        this.lblSearch = Translater.Instant("Labels.Search");
    }
    
    /// <summary>
    /// Toggles the search visibility
    /// </summary>
    public void ToggleSearch()
    {
        Visible = !Visible;
        this.StateHasChanged();
    }

    Task Search() => Searched.InvokeAsync();
}