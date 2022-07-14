using FileFlows.Shared.Widgets;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputWidget: Input<bool>
{
    [Parameter] public WidgetType Type { get; set; }

    private void ToggleValue() => this.Value = !this.Value;
}