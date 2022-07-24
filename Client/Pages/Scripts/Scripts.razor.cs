using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<string, Script>
{
    public override string ApiUrl => "/api/script";

    const string FileFlowsServer = "FileFlowsServer";
    
    private FlowSkyBox<ScriptType> Skybox;

    private Script EditingItem = null;
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private List<Script> DataFlow = new();
    private List<Script> DataProcess = new();
    private ScriptType SelectedType = ScriptType.Flow;

    private ScriptBrowser ScriptBrowser { get; set; }


    private async Task Add()
    {
        await Edit(new Script());
    }


    async Task<bool> Save(ExpandoObject model)
    {
#if (DEMO)
        return true;
#else
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
#endif
    }

    private async Task Export()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/script/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".js" });
    }

    private async Task Import()
    {
        var idResult = await ImportDialog.Show("js");
        string js = idResult.content;
        if (string.IsNullOrEmpty(js))
            return;

        Blocker.Show();
        try
        {
            var newItem = await HttpHelper.Post<Script>("/api/script/import?filename=" + UrlEncoder.Create().Encode(idResult.filename), js);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Scripts.Messages.Imported",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Invalid script");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }


    private async Task Duplicate()
    {
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/script/duplicate/{item.Uid}?type={SelectedType}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Script.Messages.Duplicated",
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
    }

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Scripts.Messages.DeleteUsed");
            return;
        }

        await base.Delete();
        await Refresh();
    }


    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
    
    
    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFlow = this.Data.Where(x => x.Type == ScriptType.Flow).ToList();
        this.DataProcess = this.Data.Where(x => x.Type == ScriptType.System).ToList();
        foreach (var script in this.Data)
        {
            if (script.Code?.StartsWith("// path: ") == true)
                script.Code = Regex.Replace(script.Code, @"^\/\/ path:(.*?)$", string.Empty, RegexOptions.Multiline).Trim();
        }
        this.Skybox.SetItems(new List<FlowSkyBoxItem<ScriptType>>()
        {
            new ()
            {
                Name = "Flow Scripts",
                Icon = "fas fa-sitemap",
                Count = this.DataFlow.Count,
                Value = ScriptType.Flow
            },
            new ()
            {
                Name = "System Scripts",
                Icon = "fas fa-microchip",
                Count = this.DataProcess.Count,
                Value = ScriptType.System
            }
        }, this.SelectedType);
    }
    
    async Task Browser()
    {
        bool result = await ScriptBrowser.Open(this.SelectedType);
        if (result)
            await this.Refresh();
    }

    
    private void SetSelected(FlowSkyBoxItem<ScriptType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }
}