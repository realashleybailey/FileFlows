namespace ViWatcher.Client.Components 
{
    using Microsoft.AspNetCore.Components;
    using viFlowPart = ViWatcher.Shared.Models.FlowPart;

    public partial class FlowElement:ComponentBase
    {
        [Parameter]
        public viFlowPart Part{ get; set; }   

        [CascadingParameter]
        public Pages.Flow Flow { get; set; }

        [Parameter]
        public bool Selected{ get; set; }

        [Parameter]
        public EventCallback<viFlowPart> OnSelect{ get; set; }
        
        private void Select(){
            this.OnSelect.InvokeAsync(this.Part);
        }
    }

}