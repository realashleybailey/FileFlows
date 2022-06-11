namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;
    using System.Collections.Generic;

    public partial class FlowTabs : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        public FlowTab ActiveTab { get; internal set; }

        List<FlowTab> Tabs = new List<FlowTab>();

        internal void AddTab(FlowTab tab)
        {
            if (Tabs.Contains(tab) == false)
            {
                Tabs.Add(tab);
                this.StateHasChanged();
            }
        }

        private void SelectTab(FlowTab tab)
        {
            this.ActiveTab = tab;
        }

        /// <summary>
        /// Called when the visibility of a tab has changed
        /// </summary>
        internal void TabVisibilityChanged()
        {
            this.StateHasChanged();
        }

        protected override Task OnParametersSetAsync()
        {
            if(ActiveTab == null)
                ActiveTab = Tabs.FirstOrDefault(x => x.Visible);
            return Task.CompletedTask;
        }

    }
}
