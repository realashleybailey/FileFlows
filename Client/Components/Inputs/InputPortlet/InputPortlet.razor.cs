using FileFlows.Shared.Portlets;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputPortlet: Input<bool>
{
    [Parameter] public PortletType Type { get; set; }

    private void ToggleValue() => this.Value = !this.Value;
}