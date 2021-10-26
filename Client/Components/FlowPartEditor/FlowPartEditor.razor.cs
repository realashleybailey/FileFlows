namespace ViWatcher.Client.Components 
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using ViWatcher.Shared;
    using ViWatcher.Shared.Models;
    using Newtonsoft.Json;
    using viFlowElement = ViWatcher.Shared.Models.FlowElement;

    public partial class FlowPartEditor : ComponentBase {

        public bool Visible{ get; set; }

        private FlowPart Part{ get; set; }
        private viFlowElement Element { get; set; }

        private string Icon{ get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel;

        TaskCompletionSource OpenTask;

        protected override void OnInitialized()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblCancel = Translater.Instant("Labels.Cancel");
        }

        internal Task Open(FlowPart part, viFlowElement element)
        {
            OpenTask = new TaskCompletionSource();
            Logger.Instance.DLog("Part: " + JsonConvert.SerializeObject(part));
            this.Part = part;
            this.Element = element;
            this.Icon = Helpers.FlowHelper.GetFlowPartIcon(part.Type);
            this.Visible = true;
            this.StateHasChanged();
            return OpenTask.Task;
        }

        private void Save(){
            OpenTask.TrySetResult();
            this.Visible = false;
            this.Part = null;
        }

        private void Cancel()
        {
            OpenTask.TrySetCanceled();
            this.Visible = false;
            this.Part = null;

        }
    }
}