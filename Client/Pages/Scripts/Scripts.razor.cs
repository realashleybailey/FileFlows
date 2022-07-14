using System.Text.Encodings.Web;
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

    private Script EditingItem = null;
    [Inject] public IJSRuntime jsRuntime { get; set; }



    private async Task Add()
    {
#if (!DEMO)
        await Edit(new Script());
#endif
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
#if (!DEMO)
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/script/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".js" });
#endif
    }

    private async Task Import()
    {
#if (!DEMO)
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
#endif
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
            string url = $"/api/script/duplicate/{item.Uid}";
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
#endif
    }

    public override async Task Delete()
    {
        var system = Table.GetSelected()?.Any(x => x.System) == true;
        if (system)
        {
            Toast.ShowError("Pages.Scripts.Messages.DeleteSystem");
            return;
        }
        
        
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Scripts.Messages.DeleteUsed");
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