namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;
    using System.Collections.Generic;

    public partial class FlowTable<TItem>: ComponentBase
    {
        [Parameter]
        public List<TItem> Data { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
        //public RenderFragment<List<FlowTableColumn<TItem>>> ChildContent { get; set; }


        List<FlowTableColumn<TItem>> ActualColumns = new ();
        
        internal void AddColumn(FlowTableColumn<TItem> col)
        {
            if (ActualColumns.Contains(col) == false)
                ActualColumns.Add(col);
        }
    }
}
