namespace FileFlow.Client.Components
{
    using System.Threading.Tasks;
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
                Icon = value == null ? "" : Helpers.FlowHelper.GetFlowPartIcon(value.Type);
            }
        }

        private string Icon { get; set; }

        [CascadingParameter]
        public Pages.Flow Flow { get; set; }

        [Parameter]
        public bool Selected { get; set; }

        [Parameter]
        public EventCallback<ffPart> OnSelect { get; set; }

        [Parameter]
        public EventCallback<ffPart> OnEdit { get; set; }

        private void Select()
        {
            this.OnSelect.InvokeAsync(this.Part);
        }

        private void Edit()
        {
            Logger.Instance.DLog("edit part!");
            this.OnEdit.InvokeAsync(this.Part);
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