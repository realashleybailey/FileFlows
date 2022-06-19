using BlazorDateRangePicker;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// A search page for library files
/// </summary>
public partial class LibraryFilesSearch : ListPage<LibraryFile>
{
    [Inject] private INavigationService NavigationService { get; set; }
    public override string ApiUrl => "/api/library-file";
    private string Title;
    private string lblSearch, lblSearching;
    private string NameMinWidth = "20ch";
    private bool Searched = false;
    [Inject] private IJSRuntime jsRuntime { get; set; }
    SearchPane SearchPane { get; set; }
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty,
        FromDate = DateTime.MinValue,
        ToDate = DateTime.MaxValue,
        Limit = 1000,
        LibraryName = string.Empty
    };
    
    protected async override Task OnInitializedAsync()
    {
        base.OnInitialized();
        this.lblSearch = Translater.Instant("Labels.Search");
        this.Title = Translater.Instant("Pages.LibraryFiles.Title");
        this.lblSearching = Translater.Instant("Labels.Searching");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('lfs-path').focus()");
    }

    public override async Task<bool> Edit(LibraryFile item)
    {
        await Helpers.LibraryFileEditor.Open(Blocker, Editor, item.Uid);
        return false;
    }
    async Task Search()
    {
        this.Searched = true;
        Blocker.Show(lblSearching);
        await Refresh();
        Blocker.Hide();
    }

    Task Closed() => NavigationService.NavigateTo("/library-files");
    
    public void OnRangeSelect(DateRange range)
    {
        SearchModel.FromDate = range.Start.Date;
        SearchModel.ToDate = range.End.Date;
    }

    public override Task Load(Guid? selectedUid = null)
    {
        if (Searched == false)
            return Task.CompletedTask;
        return base.Load(selectedUid);
    }

    protected override Task<RequestResult<List<LibraryFile>>> FetchData()
    {
        return HttpHelper.Post<List<LibraryFile>>($"{ApiUrl}/search", SearchModel);
    }
}