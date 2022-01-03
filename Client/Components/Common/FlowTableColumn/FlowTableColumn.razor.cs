namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;

    public partial class FlowTableColumn<TItem>:ComponentBase
    {
        [CascadingParameter] FlowTable<TItem> Table { get; set; }

        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment<TItem> Cell { get; set; }

        [Parameter]
        public bool Hidden { get; set; }
        [Parameter] public bool Pre { get; set; }

        string _Width = string.Empty;
        string className = "fillspace";
        string style = string.Empty;
        [Parameter]
        public string Width
        {
            get => _Width;
            set
            {
                _Width = value ?? string.Empty;
                if (_Width != string.Empty) {
                    className = string.Empty;
                    //style = $"width:{_Width};min-width:{_Width};max-width:{_Width}";
                    //style = string.Empty;
                }
                else{
                    //style = string.Empty;
                }
            }
        }

        [Parameter]
        public string MobileWidth { get; set; }

        private string _MinWidth = string.Empty;    
        [Parameter]
        public string MinWidth
        {
            get => _MinWidth;
            set
            {
                _MinWidth = value ?? string.Empty;
                if (_MinWidth == string.Empty)
                    style = string.Empty;
                else
                    style = $"min-width: {_MinWidth};";

            }
        }

        [Parameter]
        public string ColumnName { get; set; }


        public string ClassName => className;
        public string Style => style;

        protected override void OnInitialized()
        {
            this.Table.AddColumn(this);
        }

    }
}
