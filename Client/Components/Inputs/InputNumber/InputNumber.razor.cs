namespace FileFlow.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    public partial class InputNumber : Input<int>
    {
        public override bool Focus() => FocusUid();

    }
}