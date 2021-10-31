namespace FileFlow.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputText : Input<string>
    {
        public override bool Focus() => FocusUid();
    }
}