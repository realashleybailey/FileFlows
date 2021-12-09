namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;

    public partial class FlowTab:ComponentBase
    {
        [CascadingParameter] FlowTabs Tabs { get; set; }

        [Parameter] public string Title { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override void OnInitialized()
        {
            Tabs.AddTab(this);
        }

        private bool IsActive() => this.Tabs.ActiveTab == this;
    }
}
