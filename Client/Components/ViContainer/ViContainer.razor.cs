namespace ViWatcher.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using ViWatcher.Client.Shared;

    public partial class ViContainer 
    {
        [Parameter]
        public string Title{ get; set; }

        [Parameter]
        public string Icon{ get; set; }


        [Parameter]
        public RenderFragment Head { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }
        
    }
}