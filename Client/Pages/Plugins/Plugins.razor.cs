namespace FileFlows.Client.Pages
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen;
    using Radzen.Blazor;
    using FileFlows.Client.Components;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;

    public partial class Plugins : ListPage<PluginInfoModel>
    {
        public override string ApiUrl => "/api/plugin";

        protected override void OnInitialized()
        {
            _ = Load();
        }


        async Task Add()
        {
#if (!DEMO)
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Get<List<PluginPackageInfo>>(ApiUrl + "/plugin-packages");
                if (result.Success)
                    Logger.Instance.DLog("plugins", result.Data);
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        async Task Enable(bool enabled, PluginInfo plugin)
        {
#if (!DEMO)
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Put<PluginInfo>($"{ApiUrl}/state/{plugin.Uid}?enable={enabled}");
                if (result.Success)
                    plugin.Enabled = result.Data.Enabled;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        async Task Update()
        {
#if (!DEMO)
            var plugin = Table.GetSelected()?.FirstOrDefault();
            if (plugin == null)
                return;
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Post($"{ApiUrl}/update/{plugin.Uid}");
                await this.Load();
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        private PluginInfo EditingPlugin = null;

        async Task<bool> SaveSettings(ExpandoObject model)
        {
#if (!DEMO)
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var pluginResult = await HttpHelper.Post<PluginInfo>($"{ApiUrl}/{EditingPlugin.Uid}/settings", model);
                if (pluginResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("ErrorMessages.SaveFailed"));
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
            if (plugin?.HasSettings != true)
                return false;
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();

            try
            {
                var pluginResult = await HttpHelper.Get<PluginInfo>($"{ApiUrl}/{plugin.Uid}");
                if (pluginResult.Success == false)
                    return false;
                plugin.Settings = pluginResult.Data.Settings;
                plugin.Fields = pluginResult.Data.Fields;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
            this.EditingPlugin = plugin;
            var result = await Editor.Open("Plugins." + plugin.Assembly.Replace(".dll", ""), plugin.Name, plugin.Fields, plugin.Settings,
                saveCallback: SaveSettings);
            return false; // we dont need to reload the list
#else
            return false;
#endif
        }
    }

}