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
    using FileFlows.Client.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;

    public partial class Plugins : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }
        [Inject] NotificationService NotificationService { get; set; }
        protected RadzenDataGrid<PluginInfo> DataGrid { get; set; }

        private List<PluginInfo> Data = new List<PluginInfo>();

        private IList<PluginInfo> SelectedItems;
        private string lblEdit;

        const string API_URL = "/api/plugin";
        protected override void OnInitialized()
        {
            lblEdit = Translater.Instant("Labels.Edit");
            _ = Load();
        }

        async Task Load()
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await HttpHelper.Get<List<PluginInfo>>(API_URL);
                if (result.Success)
                    this.Data = result.Data;
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

        private PluginInfo EditingPlugin = null;
        private async Task RowDoubleClicked(DataGridRowMouseEventArgs<PluginInfo> item)
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