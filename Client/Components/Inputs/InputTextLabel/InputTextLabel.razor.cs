namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputTextLabel : Input<string>
    {
        [Parameter] public bool Pre { get; set; }
        [Parameter] public bool Link { get; set; }
    }
}