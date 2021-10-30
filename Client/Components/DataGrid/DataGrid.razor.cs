namespace FileFlow.Client.Components
{
    using FileFlow.Shared.Models;
    using Microsoft.AspNetCore.Components;
    public partial class DataGrid<T> where T : ViObject
    {
        [Parameter] public RenderFragment Buttons { get; set; }
        [Parameter] public RenderFragment Columns { get; set; }

        [Parameter] public Pages.ListPage<T> Page { get; set; }
    }
}