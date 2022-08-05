namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;

    public partial class FlowTab:ComponentBase
    {
        [CascadingParameter] FlowTabs Tabs { get; set; }

        private bool _Visible = true;

        [Parameter]
        public bool Visible
        {
            get => _Visible;
            set
            {
                if(_Visible == value)
                    return;
                _Visible = value;
                Tabs?.TabVisibilityChanged();
                this.StateHasChanged();
            }
        }

        private string _Title;

        [Parameter]
        public string Title
        {
            get => _Title;
            set
            {
                _Title = Translater.TranslateIfNeeded(value);
            }
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override void OnInitialized()
        {
            Tabs.AddTab(this);
        }

        private bool IsActive() => this.Tabs.ActiveTab == this;
    }
}
