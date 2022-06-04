using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<Script>
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
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", new object[] { url, item.Name + ".json" });
#endif
    }

    private async Task Import()
    {
#if (!DEMO)
        var json = await ImportDialog.Show();
        if (string.IsNullOrEmpty(json))
            return;

        Blocker.Show();
        try
        {
            var newFlow = await HttpHelper.Post<Script>("/api/script/import", json);
            if (newFlow != null && newFlow.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Script.Messages.Imported",
                    new { name = newFlow.Data.Name }));
            }
            else
            {
                Toast.ShowError(newFlow.Body?.EmptyAsNull() ?? "Invalid script");
            }
        }
        finally
        {
            Blocker.Hide();
        }
#endif
    }

}