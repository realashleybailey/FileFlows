using Microsoft.AspNetCore.Components;

namespace ViWatcher.Client.Components{
    public partial class PageView 
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