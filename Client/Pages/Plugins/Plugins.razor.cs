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

    public partial class Plugins : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }
        [Inject] NotificationService NotificationService { get; set; }
        protected RadzenDataGrid<PluginInfoModel> DataGrid { get; set; }

        private List<PluginInfoModel> Data = new List<PluginInfoModel>();

        private IList<PluginInfoModel> SelectedItems;
        private string lblAdd, lblEdit, lblUpdate;

        const string API_URL = "/api/plugin";
        protected override void OnInitialized()
        {
            lblAdd = Translater.Instant("Labels.Add");
            lblEdit = Translater.Instant("Labels.Edit");
            lblUpdate = Translater.Instant("Labels.Update");
            _ = Load();
        }

        async Task Load()
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Get<List<PluginInfoModel>>(API_URL);
                if (result.Success)
                    this.Data = result.Data;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        async Task Add()
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Get<List<PluginPackageInfo>>(API_URL + "/plugin-packages");
                if (result.Success)
                    Logger.Instance.DLog("plugins", result.Data);
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }

        }

        async Task Enable(bool enabled, PluginInfo plugin)
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Put<PluginInfo>($"{API_URL}/state/{plugin.Uid}?enable={enabled}");
                if (result.Success)
                    plugin.Enabled = result.Data.Enabled;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        async Task Update()
        {
            var plugin = SelectedItems.FirstOrDefault();
            if (plugin == null)
                return;
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Post($"{API_URL}/update/{plugin.Uid}");
                await this.Load();
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        private PluginInfo EditingPlugin = null;
        private async Task RowDoubleClicked(DataGridRowMouseEventArgs<PluginInfoModel> item)
        {
            this.SelectedItems.Clear();
            this.SelectedItems.Add(item.Data);
            await Edit();
        }
        async Task Edit()
        {
            PluginInfo plugin = this.SelectedItems.FirstOrDefault();
            if (plugin?.HasSettings != true)
                return;
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();

            try
            {
                var pluginResult = await HttpHelper.Get<PluginInfo>($"{API_URL}/{plugin.Uid}");
                if (pluginResult.Success == false)
                    return;
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
        }

        async Task<bool> SaveSettings(ExpandoObject model)
        {
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var pluginResult = await HttpHelper.Post<PluginInfo>($"{API_URL}/{EditingPlugin.Uid}/settings", model);
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
        }
    }

}