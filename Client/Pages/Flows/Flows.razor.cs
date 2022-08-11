using System.Reflection;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Inputs;
using Microsoft.AspNetCore.Components.Rendering;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.JSInterop;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ffFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public partial class Flows : ListPage<Guid, FlowListModel>
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] public IJSRuntime jsRuntime { get; set; }

    private NewFlowEditor AddEditor;
    private string TableIdentifier => "Flows-" + this.SelectedType;

    public override string ApiUrl => "/api/flow";

    private FlowSkyBox<FlowType> Skybox;

    private List<FlowListModel> DataStandard = new();
    private List<FlowListModel> DataFailure = new();
    private FlowType SelectedType = FlowType.Standard;

    #if(DEBUG)
    private bool DEBUG = true;
    #else
    private bool DEBUG = false;
    #endif

    public override string FetchUrl => ApiUrl + "/list-all";


    async Task Enable(bool enabled, ffFlow flow)
    {
        Blocker.Show();
        try
        {
            await HttpHelper.Put<ffFlow>($"{ApiUrl}/state/{flow.Uid}?enable={enabled}");
        }
        finally
        {
            Blocker.Hide();
        }
    }

    private async void Add()
    {
        if (AddEditor == null)
        {
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
            return;
        }
        var  newFlow = await AddEditor.Show();
        if (newFlow == null)
            return; // was canceled
        
        if (newFlow.Uid != Guid.Empty)
        {
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
            {
                // refresh the app configuration status
                await App.Instance.LoadAppInfo();
            }
            // was saved, refresh list
            await this.Refresh();
        }
        else
        {
            // edit it
            App.Instance.NewFlowTemplate = newFlow;
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }
    }

    public override async Task<bool> Edit(FlowListModel item)
    {
        if(item != null)
            NavigationManager.NavigateTo("flows/" + item.Uid);
        return await Task.FromResult(false);
    }

    private async Task Export()
    {
        var items = Table.GetSelected();
        if (items?.Any() != true)
            return;
        var last = items.Last();
        foreach (var item in items)
        {
            string url = $"/api/flow/export/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".json" });
            if(item != last)
                await Task.Delay(1000); // need to actually allow the browser to download the additional flows
        }
    }

    private async Task Import()
    {
#if (!DEMO)
        var idResult = await ImportDialog.Show();
        string json = idResult.content;
        if (string.IsNullOrEmpty(json))
            return;

        Blocker.Show();
        try
        {
            var newFlow = await HttpHelper.Post<ffFlow>("/api/flow/import", json);
            if (newFlow != null && newFlow.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.FlowImported", new { name = newFlow.Data.Name }));
            }
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }

    private async Task Template()
    {
#if (DEBUG)

        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/flow/template/{item.Uid}";
        url = "http://localhost:6868" + url;
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".json" });
#endif
    }

    private class TemplateSelectParameters
    {
        public List<Plugin.ListOption> Options { get; set; }
    }
    
    
    private async Task Duplicate()
    {
#if (!DEMO)
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/flow/duplicate/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Flows.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }

    protected override Task PostDelete() => Refresh();

    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFailure = this.Data.Where(x => x.Type == FlowType.Failure).ToList();
        this.DataStandard = this.Data.Where(x => x.Type == FlowType.Standard).ToList();
        this.Skybox.SetItems(new List<FlowSkyBoxItem<FlowType>>()
        {
            new ()
            {
                Name = "Standard Flows",
                Icon = "fas fa-sitemap",
                Count = this.DataStandard.Count,
                Value = FlowType.Standard
            },
            new ()
            {
                Name = "Failure Flows",
                Icon = "fas fa-exclamation-circle",
                Count = this.DataFailure.Count,
                Value = FlowType.Failure
            }
        }, this.SelectedType);
    }

    private void SetSelected(FlowSkyBoxItem<FlowType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task SetDefault()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        
        Blocker.Show();
        try
        {
            await HttpHelper.Put($"/api/flow/set-default/{item.Uid}?default={(!item.Default)}");
            await this.Refresh();
        }
        finally
        {
            Blocker.Hide();
        }
    }

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Flows.Messages.DeleteUsed");
            return;
        }
        await base.Delete();
    }

    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
}
