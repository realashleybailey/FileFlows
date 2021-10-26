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
    using BlazorMonaco;
    using Microsoft.JSInterop;

    public partial class CodeEditor : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving;

        const string API_URL = "/api/code-eval";

        private MonacoEditor Editor{ get; set; }

        [Inject]
        private IJSRuntime jsRuntime{ get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
        }

        private async Task Save()
        {
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                string code = await Editor.GetValue();
                var resault = await HttpHelper.Post<string>(API_URL + "/validate", code);                
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
        }

        private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Minimap = new EditorMinimapOptions { Enabled = false },
                Theme = "vs-dark",
                Language = "javascript",
                Value = "function xyz() {\n" +
                        "   console.log(\"Hello world!\");\n" +
                        "}"
            };
        }

        private void OnEditorInit(MonacoEditorBase e)
        {
            Logger.Instance.DLog("editor init done");
            _ = jsRuntime.InvokeVoidAsync("ViCode.initModel");
        }
    }
}