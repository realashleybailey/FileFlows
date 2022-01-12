namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputPassword: Input<string>
    {
        public override bool Focus() => FocusUid();

        protected override void ValueUpdated()
        {
            ClearError();
        }
    }
}