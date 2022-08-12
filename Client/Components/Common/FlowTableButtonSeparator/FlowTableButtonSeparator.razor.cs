using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

public partial class FlowTableButtonSeparator
{
    [CascadingParameter] FlowTableBase Table { get; set; }
    
    protected override void OnInitialized()
    {
        if (this.Table != null)
        {
            this.Table.AddButtonSeperator();
        }
    }
}