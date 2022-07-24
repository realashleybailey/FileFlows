using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using ffElement = FileFlows.Shared.Models.FlowElement;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Client.Components.Common;


namespace FileFlows.Client.Components;

public partial class ScriptBrowser: ComponentBase
{
    const string ApiUrl = "/api/script-repo";
    [CascadingParameter] public Blocker Blocker { get; set; }
    [CascadingParameter] public Editor Editor { get; set; }

    public FlowTable<RepositoryScript> Table { get; set; }

    public bool Visible { get; set; }

    private bool Updated;

    private string lblTitle, lblClose;

    TaskCompletionSource<bool> OpenTask;

    private bool _needsRendering = false;

    private bool Loading = false;

    private ScriptType ScriptType;

    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
        lblTitle = Translater.Instant("Pages.Scripts.Labels.ScriptBrowser");
    }

    internal Task<bool> Open(ScriptType type)
    {
        this.ScriptType = type;
        lblTitle = Translater.Instant("Pages.Scripts.Labels.ScriptBrowser") + " - " + type  + " Scripts";
        this.Visible = true;
        this.Loading = true;
        this.Table.Data = new List<RepositoryScript>();
        OpenTask = new TaskCompletionSource<bool>();
        _ = LoadData();
        this.StateHasChanged();
        return OpenTask.Task;
    }

    private async Task LoadData()
    {
        this.Loading = true;
        Blocker.Show();
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Get<List<RepositoryScript>>(ApiUrl + "/scripts?missing=true&type=" + ScriptType);
            if (result.Success == false)
            {
                // close this and show message
                this.Close();
                return;
            }
            this.Table.Data = result.Data;
            this.Loading = false;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    private async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    private void Close()
    {
        OpenTask.TrySetResult(Updated);
        this.Visible = false;
    }

    private async Task Download()
    {
        var selected = Table.GetSelected().ToArray();
        var items = selected.Select(x => x.Path).ToList();
        if (items.Any() == false)
            return;
        this.Blocker.Show();
        this.StateHasChanged();
        try
        {
            this.Updated = true;
            var result = await HttpHelper.Post(ApiUrl + "/download", new { Scripts = items });
            if (result.Success == false)
            {
                // close this and show message
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error: " + ex.Message);
        }
        finally
        {
            this.Blocker.Hide();
            this.StateHasChanged();
        }
        this.Close();
        //await LoadData();
    }

    private async Task ViewAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await View(item);
    }

    private async Task View(RepositoryScript script)
    {
        Blocker.Show();
        string code;
        try
        {
            var response =
                await HttpHelper.Get<string>(ApiUrl + "/code?path=" + UrlEncoder.Create().Encode(script.Path));
            if (response.Success == false)
                return;
            code = response.Data;
        }
        finally
        {
            Blocker.Hide();
        }

        await Editor.Open("Pages.Scripts", script.Name, new List<ElementField>
        {
            new()
            {
                Name = "Code",
                InputType = FormInputType.Code
            },
        }, new { Code = code }, readOnly: true);
    }
}