namespace FileFlow.Client.Components.Inputs
{
    using Models;
    using FileFlow.Client.Helpers;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Newtonsoft.Json;
    using FileFlow.Shared;
    using FileFlow.Client.Components;
    using BlazorMonaco;
    using Microsoft.JSInterop;

    public partial class InputCode : Input<string>
    {

        const string API_URL = "/api/code-eval";

        private MonacoEditor Editor { get; set; }

        [Inject]
        private IJSRuntime jsRuntime { get; set; }


        private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Minimap = new EditorMinimapOptions { Enabled = false },
                Theme = "vs-dark",
                Language = "javascript",
                Value = this.Value ?? ""
            };
        }

        private void OnEditorInit(MonacoEditorBase e)
        {
            Logger.Instance.DLog("editor init done");
            _ = jsRuntime.InvokeVoidAsync("ffCode.initModel");
        }

        private void OnBlur()
        {
            _ = Task.Run(async () =>
            {

                this.Value = await Editor.GetValue();
            });
        }
    }
}