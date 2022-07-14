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
        public RenderFragment HeadLeft { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        /// <summary>
        /// Gets or sets if this container can be maxmised
        /// </summary>
        [Parameter] public bool Maximise { get; set; }

        /// <summary>
        /// Gets or sets event callback when the maximise is changed
        /// </summary>
        [Parameter] public EventCallback<bool> OnMaximised { get; set; }

        [Parameter]
        public bool Flex { get; set; }
        
        /// <summary>
        /// Gets or sets additional class names to add to the ViContainer
        /// </summary>
        [Parameter] public string ClassName { get; set; }

        private bool IsMaximised { get; set; }

        protected override void OnInitialized()
        {
            this.IsMaximised = false;
        }

        private void ToggleMaximise()
        {
            this.IsMaximised = !this.IsMaximised;
            OnMaximised.InvokeAsync(this.IsMaximised);
        }
    }
}