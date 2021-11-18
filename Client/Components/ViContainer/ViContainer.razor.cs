namespace FileFlows.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Shared;

    public partial class ViContainer
    {
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public bool FullWidth { get; set; }

        [Parameter]
        public bool AlwaysShowTitle { get; set; }


        [Parameter]
        public RenderFragment Head { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        [Parameter]
        public bool Flex { get; set; }

    }
}