namespace FileFlows.Client.Pages
{
    using FileFlows.Shared.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using FileFlows.Client.Components;
    using System.Collections.Generic;
    using FileFlows.Shared.Validators;
    using Microsoft.JSInterop;
    using FileFlows.Plugin;

    public partial class Settings : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime jsRuntime { get; set; }

        private bool ShowInternalProcessingNdoe { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblHelp, lblGeneral, lblAdvanced, lblNode, lblInternalProcessingNodeDescription;

        private FileFlows.Shared.Models.Settings Model { get; set; } = new FileFlows.Shared.Models.Settings();

        private FileFlows.Shared.Models.ProcessingNode InternalProcessingNode { get; set; } 

        List<Validator> DirectoryValidators = new ();


        protected override async Task OnInitializedAsync()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblHelp = Translater.Instant("Labels.Help");
            lblAdvanced = Translater.Instant("Labels.Advanced");
            lblGeneral = Translater.Instant("Pages.Settings.Labels.General");
            lblNode = Translater.Instant("Pages.Settings.Labels.InternalProcessingNode");
            lblInternalProcessingNodeDescription = Translater.Instant("Pages.Settings.Fields.InternalProcessingNode.Description");
            Blocker.Show("Loading Settings");

            DirectoryValidators.Add(new Required());

#if (!DEMO)
            var response = await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings");
            if (response.Success)
            {
                this.Model = response.Data;
            }

            var nodesResponse = await HttpHelper.Get<ProcessingNode[]>("/api/node");
            if (nodesResponse.Success)
            {
                this.InternalProcessingNode = nodesResponse.Data.Where(x => x.Address == "FileFlowsServer").FirstOrDefault();
                this.ShowInternalProcessingNdoe = this.InternalProcessingNode != null;
            }

#endif
            Blocker.Hide();
        }

        private async Task Save()
        {
#if (DEMO)
            return;
#else
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                await HttpHelper.Put<string>("/api/settings", this.Model);

                if (ShowInternalProcessingNdoe && this.InternalProcessingNode != null)
                {
                    await HttpHelper.Post("/api/node", this.InternalProcessingNode);
                }
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
#endif
        }

        private async void OpenHelp()
        {
            await jsRuntime.InvokeVoidAsync("open", "https://github.com/revenz/FileFlows/wiki/Settings", "_blank");
        }
    }
}