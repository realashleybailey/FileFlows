using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

public partial class FlowSlider:ComponentBase
{
    private readonly string Uid = Guid.NewGuid().ToString();

    protected int _Value;

    [Parameter] public int Min { get; set; } = 0;
    [Parameter] public int Max { get; set; } = 100;

    [Parameter] public string Prefix { get; set; } = string.Empty;
    [Parameter] public string Suffix { get; set; } = string.Empty;

    [Parameter]
    public int Value
    {
        get => _Value;
        set
        {
            if (_Value != value)
            {
                _Value = value;
                ValueChanged.InvokeAsync(value);
            }
        }
    }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }
}
