namespace ViWatcher.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using ViWatcher.Shared;
    using ViWatcher.Shared.Models;
    using Newtonsoft.Json;
    using viFlowElement = ViWatcher.Shared.Models.FlowElement;
    using System.Collections.Generic;

    public partial class FlowPartEditor : ComponentBase
    {

        public bool Visible { get; set; }

        private FlowPart Part { get; set; }
        private viFlowElement Element { get; set; }

        private string Icon { get; set; }
        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblCancel;

        private Dictionary<string, object> Model { get; set; }

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
            this.Model = part.Model as Dictionary<string, object> ?? element.Model as Dictionary<string, object> ?? new Dictionary<string, object>();
            this.StateHasChanged();
            return OpenTask.Task;
        }

        private void Save()
        {
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

        private void UpdateValue(string field, object value)
        {
            if (this.Model.ContainsKey(field))
                this.Model[field] = value;
            else
                this.Model.Add(field, value);
        }

        private T GetValue<T>(string field, T @default = default(T))
        {
            Logger.Instance.DLog("Getting value for: " + field);
            if (this.Model.ContainsKey(field) == false)
            {
                Logger.Instance.DLog("Not in model");
                return @default;
            }
            object value = this.Model[field];
            if (value == null)
            {
                Logger.Instance.DLog("value is null");
                return @default;
            }
            if (value is T)
            {
                return (T)value;
            }
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                Logger.Instance.DLog("Not of type: " + value.GetType());
                return @default;

            }
        }
    }
}