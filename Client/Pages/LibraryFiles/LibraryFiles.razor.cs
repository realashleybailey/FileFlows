using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;

namespace FileFlows.Client.Pages;

public partial class LibraryFiles : ListPage<Guid, LibaryFileListModel>
{
    public override string ApiUrl => "/api/library-file";
    [Inject] private INavigationService NavigationService { get; set; }
    [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }

    [Inject] private IJSRuntime jsRuntime { get; set; }

    private FlowSkyBox<FileStatus> Skybox;

    private FileFlows.Shared.Models.FileStatus SelectedStatus;

    private int PageIndex;

    private string lblMoveToTop = "";

    private int Count;
    private string lblSearch;

    private string TableIdentifier => "LibraryFiles_" + this.SelectedStatus; 

    SearchPane SearchPane { get; set; }
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty
    };

    private string Title;
    private string lblLibraryFiles, lblFileFlowsServer;
    private int TotalItems;
    
    private async Task SetSelected(FlowSkyBoxItem<FileStatus> status)
    {
        SelectedStatus = status.Value;
        this.PageIndex = 0;
        Title = lblLibraryFiles + ": " + status.Name;
        await this.Refresh();
        Logger.Instance.ILog("SetSelected: " + this.Data?.Count);
        this.StateHasChanged();
    }

    public override string FetchUrl => $"{ApiUrl}/list-all?status={Skybox?.SelectedItem?.Value}&page={PageIndex}&pageSize={App.PageSize}";

    private string NameMinWidth = "20ch";

    public override async Task PostLoad()
    {
        if(App.Instance.IsMobile)
            this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length / 2) ?? 0))) + "ch" : "20ch";
        else
            this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length) ?? 0))) + "ch" : "20ch";
        Logger.Instance.ILog("PostLoad: " + this.Data.Count);
        CheckPager();
        await jsRuntime.InvokeVoidAsync("ff.scrollTableToTop");
    }

    protected override async Task PostDelete()
    {
        await RefreshStatus();
    }

    protected async override Task OnInitializedAsync()
    {
        base.OnInitialized();
        this.SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;
        lblMoveToTop = Translater.Instant("Pages.LibraryFiles.Buttons.MoveToTop");
        lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
        lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        Title = lblLibraryFiles + ": " + Translater.Instant("Enums.FileStatus." + FileStatus.Unprocessed);
        this.lblSearch = Translater.Instant("Labels.Search");

    }

    private Task<RequestResult<List<LibraryStatus>>> GetStatus() => HttpHelper.Get<List<LibraryStatus>>(ApiUrl + "/status");

    /// <summary>
    /// Refreshes the top status bar
    /// This is needed when deleting items, as the list will not be refreshed, just items removed from it
    /// </summary>
    /// <returns></returns>
    private async Task RefreshStatus()
    {
        var result = await GetStatus();
        if (result.Success)
            RefreshStatus(result.Data.ToList());
    }
    
    private void RefreshStatus(List<LibraryStatus> data)
    {
       var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
       foreach (var s in order)
       {
           if (data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
               data.Add(new LibraryStatus { Status = s });
       }

       foreach (var s in data)
           s.Name = Translater.Instant("Enums.FileStatus." + s.Status.ToString());

       var sbItems = new List<FlowSkyBoxItem<FileStatus>>();
       foreach (var status in data.OrderBy(x =>
                {
                    int index = order.IndexOf(x.Status);
                    return index >= 0 ? index : 100;
                }))
       {
           string icon = status.Status switch
           {
               FileStatus.Unprocessed => "far fa-hourglass",
               FileStatus.Disabled => "fas fa-toggle-off",
               FileStatus.Processed => "far fa-check-circle",
               FileStatus.Processing => "fas fa-file-medical-alt",
               FileStatus.FlowNotFound => "fas fa-exclamation",
               FileStatus.ProcessingFailed => "far fa-times-circle",
               FileStatus.OutOfSchedule => "far fa-calendar-times",
               FileStatus.Duplicate => "far fa-copy",
               FileStatus.MappingIssue => "fas fa-map-marked-alt",
               FileStatus.MissingLibrary => "fas fa-trash",
               FileStatus.OnHold => "fas fa-hand-paper",
               _ => ""
           };
           if (status.Status != FileStatus.Unprocessed && status.Status != FileStatus.Processing && status.Status != FileStatus.Processed && status.Count == 0)
               continue;
           sbItems.Add(new ()
           {
               Count = status.Count,
               Icon = icon,
               Name = status.Name,
               Value = status.Status
           });
        }

        Skybox.SetItems(sbItems, SelectedStatus);
        this.Count = sbItems.Where(x => x.Value == SelectedStatus).Select(x => x.Count).FirstOrDefault();
        CheckPager();
        this.StateHasChanged();
    }

    public override async Task<bool> Edit(LibaryFileListModel item)
    {
        await Helpers.LibraryFileEditor.Open(Blocker, Editor, item.Uid);
        return false;
    }

    public async Task MoveToTop()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/move-to-top", new ReferenceModel<Guid> { Uids = uids });                
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    public async Task Cancel()
    {
        var selected = Table.GetSelected().ToArray();
        if (selected.Length == 0)
            return; // nothing to cancel

        if (await Confirm.Show("Labels.Cancel",
            Translater.Instant("Labels.CancelItems", new { count = selected.Length })) == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();
        try
        {
            foreach(var item in selected)
                await HttpHelper.Delete($"/api/worker/by-file/{item.Uid}");

        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        await Refresh();
    }

    public async Task Reprocess()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to reprocess

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/reprocess", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }

    protected override async Task<RequestResult<List<LibaryFileListModel>>> FetchData()
    {
        var request = await HttpHelper.Get<LibraryFileDatalistModel>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<LibaryFileListModel>>
            {
                Body = request.Body,
                Success = request.Success
            };
        }

        RefreshStatus(request.Data?.Status?.ToList() ?? new List<LibraryStatus>());
        
        var result = new RequestResult<List<LibaryFileListModel>>
        {
            Body = request.Body,
            Success = request.Success,
            Data = request.Data.LibraryFiles.ToList()
        };
        Logger.Instance.ILog("FetchData: " + result.Data.Count);
        return result;
    }

    /// <summary>
    /// Checks if the pager should be visible on the amount of data
    /// </summary>
    private void CheckPager()
    {
        var status = Skybox.SelectedItem;
        this.TotalItems = status?.Count ?? 0;
        Logger.Instance.ILog("TotalItems: " + TotalItems);
        this.StateHasChanged();
    }

    private async Task PageChange(int index)
    {
        PageIndex = index;
        await this.Refresh();
    }

    private async Task PageSizeChange(int size)
    {
        this.PageIndex = 0;
        await this.Refresh();
    }

    private async Task Rescan()
    {
        this.Blocker.Show("Scanning Libraries");
        try
        {
            await HttpHelper.Post("/api/library/rescan-enabled");
            await Refresh();
        }
        finally
        {
            this.Blocker.Hide();   
        }
    }

    private async Task Unhold()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to unhold

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/unhold", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    
    Task Search() => NavigationService.NavigateTo("/library-files/search");

}