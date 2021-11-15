namespace FileFlow.Client.Components
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using FileFlow.Shared;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using ffPart = FileFlow.Shared.Models.FlowPart;

    public partial class FlowElement : ComponentBase
    {
        private ffPart _Part;
        [Parameter]
        public ffPart Part
        {
            get => _Part;
            set
            {
                _Part = value;
                Icon = value == null ? "" : value?.Icon?.EmptyAsNull() ?? Helpers.FlowHelper.GetFlowPartIcon(value.Type);
            }
        }

        private string Icon { get; set; }

        [CascadingParameter]
        public Pages.Flow Flow { get; set; }

        [Parameter]
        public bool Selected { get; set; }

        [Parameter]
        public EventCallback<ffPart> OnSelect { get; set; }

        public string Label { get; set; }

        protected override void OnInitialized()
        {
            this.Label = Helpers.FlowHelper.FormatLabel(Part.Name);
        }

        private void Select()
        {
            this.OnSelect.InvokeAsync(this.Part);
        }

        private async Task Edit()
        {
            Logger.Instance.DLog("edit part!");
            bool updated = await Flow.Edit(this.Part);
            if (updated)
            {
                Logger.Instance.DLog("flow part updated!, recheck number of outputs!");
                this.StateHasChanged();
            }
        }

        private async Task KeyDown(KeyboardEventArgs e)
        {
            if (e.AltKey || e.CtrlKey || e.ShiftKey)
                return;
            if (e.Key == "Delete")
            {
                // delete this part
                await Flow.DeletePart(this.Part);
            }
            else if (e.Key == "Enter")
            {
                Edit();
            }
        }
    }

}