namespace FileFlows.Client.Pages
{
    using FileFlows.Shared.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using FileFlows.Client.Components;

    public partial class Settings : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving;

        private FileFlows.Shared.Models.Settings Model { get; set; } = new FileFlows.Shared.Models.Settings();

        protected override async Task OnInitializedAsync()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            Blocker.Show("Loading Settings");

            var response = await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings");
            if (response.Success)
            {
                this.Model = response.Data;
            }

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