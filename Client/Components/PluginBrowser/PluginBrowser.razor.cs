namespace FileFlows.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using ffElement = FileFlows.Shared.Models.FlowElement;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Reflection;
    using FileFlows.Plugin.Attributes;
    using System.Linq;
    using System.ComponentModel;
    using FileFlows.Client.Components.Inputs;
    using FileFlows.Plugin;
    using System.Text.Json;
    using FileFlows.Client.Components.Common;

    public partial class PluginBrowser : ComponentBase
    {
        const string ApiUrl = "/api/plugin";
        [CascadingParameter] public Blocker Blocker { get; set; }
        [CascadingParameter] public Editor Editor { get; set; }

        public FlowTable<PluginPackageInfo> Table { get; set; }

        public bool Visible { get; set; }

        private bool Updated;

        private string lblTitle, lblClose;

        TaskCompletionSource<bool> OpenTask;

        private bool _needsRendering = false;

        private bool Loading = false;

        protected override void OnInitialized()
        {
            lblClose = Translater.Instant("Labels.Close");
            lblTitle = Translater.Instant("Pages.Plugins.Labels.PluginBrowser");
        }

        internal Task<bool> Open()
        {
            this.Visible = true;
            this.Loading = true;
            this.Table.Data = new List<PluginPackageInfo>();
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
                var result = await HttpHelper.Get<List<PluginPackageInfo>>(ApiUrl + "/plugin-packages?missing=true");
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
            var items = selected.Select(x => x.Package).ToList();
            if (items.Any() == false)
                return;
            this.Blocker.Show();
            this.StateHasChanged();
            try
            {
                this.Updated = true;
                var result = await HttpHelper.Post(ApiUrl + "/download", new { Packages = items });
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
            await LoadData();
        }

        private async Task ViewAction()
        {
            var item = Table.GetSelected().FirstOrDefault();
            if (item != null)
                await View(item);
        }

        private async Task View(PluginPackageInfo plugin)
        {
            await Editor.Open("Pages.Plugins", plugin.Name, new List<ElementField>
            {
                new ElementField
                {
                    Name = nameof(plugin.Name),
                    InputType = FormInputType.Text,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Authors),
                    InputType = FormInputType.Text,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Version),
                    InputType = FormInputType.Text,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Url),
                    InputType = FormInputType.Text,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Description),                    
                    InputType = FormInputType.TextArea,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true }
                    }
                },
                new ElementField
                {
                    Name = nameof(plugin.Elements),
                    InputType = FormInputType.Checklist,
                    Parameters = new Dictionary<string, object>
                    {
                        { nameof(InputText.ReadOnly), true },
                        { 
                            nameof(InputChecklist.Options), 
                            plugin.Elements.Select(x => new ListOption{ Label = x, Value = x }).ToList()
                        }
                    }
                },
            }, plugin, readOnly: true);
        }

    }
}