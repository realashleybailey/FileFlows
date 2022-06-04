using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// An Input for a Button
/// </summary>
public partial class InputButton : Input<object>
{
    /// <summary>
    /// Gets or sets the Text of the button
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// Gets or sets the event that is triggered when the button is clicked
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }
}