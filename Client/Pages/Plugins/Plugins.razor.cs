using FileFlows.Client.Components.Dialogs;

using FileFlows.Client.Components;
using FileFlows.Plugin;
using FileFlows.Client.Components.Inputs;
using System.Text.Json;

namespace FileFlows.Client.Pages;

public partial class Plugins : ListPage<Guid, PluginInfoModel>
{
    public override string ApiUrl => "/api/plugin";

    private PluginBrowser PluginBrowser { get; set; }

    protected override void OnInitialized()
    {
        _ = Load(default);
    }

    protected override string DeleteMessage => "Pages.Plugins.Messages.DeletePlugins";

    async Task Add()
    {
#if (!DEMO)
        bool result = await PluginBrowser.Open();
        if (result)
            await PluginsUpdated();
#endif
    }

    async Task Update()
    {
#if (!DEMO)
        var plugins = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (plugins?.Any() != true)
            return;
        Blocker.Show();
        this.StateHasChanged();
        Data.Clear();
        try
        {
            var result = await HttpHelper.Post($"{ApiUrl}/update", new ReferenceModel<Guid> { Uids = plugins });
            if (result.Success)
                await PluginsUpdated();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
#endif
    }

    async Task PluginsUpdated()
    {
        await App.Instance.LoadLanguage();
        await this.Load(default);
    }

    private PluginInfo EditingPlugin = null;

    async Task<bool> SaveSettings(ExpandoObject model)
    {
#if (!DEMO)
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(model);
            var pluginResult = await HttpHelper.Post($"{ApiUrl}/{EditingPlugin.PackageName}/settings", json);
            if (pluginResult.Success == false)
            {
                Toast.ShowError( Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }
            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
#else
        return true;
#endif
    }

    public override async Task<bool> Edit(PluginInfoModel plugin)
    {
#if (!DEMO)
        if (plugin?.Settings?.Any() != true)
            return false;
        Blocker.Show();
        this.StateHasChanged();
        Data.Clear();

        ExpandoObject model = new ExpandoObject();
        try
        {
            var pluginResult = await HttpHelper.Get<string>($"{ApiUrl}/{plugin.PackageName}/settings");
            if (pluginResult.Success == false)
                return false;
            if (string.IsNullOrWhiteSpace(pluginResult.Data) == false)
                model = JsonSerializer.Deserialize<ExpandoObject>(pluginResult.Data);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        this.EditingPlugin = plugin;

        // clone the fields as they get wiped
        var fields = plugin.Settings.ToList();

        var result = await Editor.Open(new()
        {
            TypeName = "Plugins." + plugin.PackageName, Title = plugin.Name, Fields = fields, Model = model,
            SaveCallback = SaveSettings
        });
        return false; // we dont need to reload the list
#else
        return false;
#endif
    }


    private async Task AboutAction()
    {
        var item = Table.GetSelected().FirstOrDefault();
        if (item != null)
            await About(item);
    }
    private async Task DoubleClick(PluginInfoModel plugin)
    {
        if (plugin.Settings?.Any() == true)
            await Edit(plugin);
        else
            await About(plugin);
    }

    private async Task About(PluginInfoModel plugin)
    {
        await Editor.Open(new()
        {
            TypeName = "Pages.Plugins", Title = plugin.Name, Fields = new List<ElementField>
            {
                new ElementField
                {
                    Name = nameof(plugin.Name),
                    InputType = FormInputType.TextLabel
                },
                new ElementField
                {
                    Name = nameof(plugin.Authors),
                    InputType = FormInputType.TextLabel
                },
                new ElementField
                {
                    Name = nameof(plugin.Version),
                    InputType = FormInputType.TextLabel
                },
                new ElementField
                {
                    Name = nameof(plugin.Url),
                    InputType = FormInputType.TextLabel,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputTextLabel.Link), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Description),
                    InputType = FormInputType.TextLabel,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputTextLabel.Pre), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Elements),
                    InputType = FormInputType.Checklist,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputChecklist.ListOnly), true },
                        {
                            nameof(InputChecklist.Options),
                            plugin.Elements?.Select(x => new ListOption { Label = x.Name, Value = x })?.ToList() ??
                            new List<ListOption>()
                        }
                    }
                },
            },
            Model = plugin, ReadOnly = true
        });
    }

    
    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            Toast.ShowError("Pages.Plugins.Messages.DeleteUsed");
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