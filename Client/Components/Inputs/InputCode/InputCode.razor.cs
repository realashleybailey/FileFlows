namespace FileFlows.Client.Components.Inputs
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using BlazorMonaco;
    using Microsoft.JSInterop;
    using System.Collections.Generic;
    using FileFlows.Plugin;

    public partial class InputCode : Input<string>
    {

        const string API_URL = "/api/code-eval";
        private bool Updating = false;

        private MonacoEditor CodeEditor { get; set; }

        private Dictionary<string, object> _Variables = new Dictionary<string, object>();
        [Parameter]
        public Dictionary<string, object> Variables
        {
            get => _Variables;
            set { _Variables = value ?? new Dictionary<string, object>(); }
        }

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
            _ = jsRuntime.InvokeVoidAsync("ffCode.initModel", Variables);
        }

        private void OnBlur()
        {
            _ = Task.Run(async () =>
            {
                this.Updating = true;
                this.Value = await CodeEditor.GetValue();
                this.Updating = false;
            });
        }

        protected override void ValueUpdated()
        {
            if (this.Updating)
                return;
            if (string.IsNullOrEmpty(this.Value) || CodeEditor == null)
                return;
            CodeEditor.SetValue(this.Value.Trim());
        }
    }
}