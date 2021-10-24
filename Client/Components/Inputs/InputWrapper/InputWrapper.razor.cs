namespace ViWatcher.Client.Components.Inputs 
{
    using Microsoft.AspNetCore.Components;
    public partial class InputWrapper:ComponentBase
    {
        [Parameter]
        public IInput Input{ get; set; }
        
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}