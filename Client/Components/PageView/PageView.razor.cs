namespace FileFlows.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Shared;

    public partial class PageView
    {
        [CascadingParameter]
        public NavMenu Menu { get; set; }


        [Parameter]
        public RenderFragment Head { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        [Parameter]
        public bool FullWidth { get; set; }

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public bool Flex { get; set; }

    }
}