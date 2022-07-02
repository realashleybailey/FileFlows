using FileFlows.Shared.Portlets;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputPortlet: Input<bool>
{
    [Parameter]
    public bool Inverse { get; set; }

    [Parameter] public PortletType Type { get; set; }

    public new bool Value
    {
        get
        {
            if (Inverse) return !base.Value;
            return base.Value;  
        }
        set
        {
            if (Inverse) base.Value = !value;
            else base.Value = value;
        }
    }
}