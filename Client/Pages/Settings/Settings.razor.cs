namespace ViWatcher.Client.Pages 
{
    using Models;
    using ViWatcher.Client.Helpers;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Newtonsoft.Json;
    using ViWatcher.Shared;
    using ViWatcher.Client.Components;

    public partial class Settings : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblSource, lblDestination;

        private ViWatcher.Shared.Models.Settings Model { get; set; } = new ViWatcher.Shared.Models.Settings();

        private ListOption[] Containers;

        protected override async Task OnInitializedAsync()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblSource = Translater.Instant("Pages.Settings.Fields.Source");
            lblDestination = Translater.Instant("Pages.Settings.Fields.Destination");

            Containers = new[] {
                new ListOption { Value = "mkv", Label = "MKV"},
                new ListOption { Value = "mp4", Label = "MP4"},
            };

            Blocker.Show("Loading Settings");

            var response  = await HttpHelper.Get<ViWatcher.Shared.Models.Settings>("/api/settings");
            if(response.Success)
                this.Model = response.Data;

            Blocker.Hide();
        }

        private async Task Save()
        {
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                await HttpHelper.Put<string>("/api/settings", this.Model);
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
        }
    }
}