namespace FileFlow.Client.Pages
{
    using FileFlow.Client.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;
    using FileFlow.Client.Components;

    using FileFlow.Plugin;
   public partial class Settings : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblSource, lblDestination;

        private FileFlow.Shared.Models.Settings Model { get; set; } = new FileFlow.Shared.Models.Settings();

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

            var response = await HttpHelper.Get<FileFlow.Shared.Models.Settings>("/api/settings");
            if (response.Success)
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