namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    public partial class InputTextArea : Input<string>
    {
        public override bool Focus() => FocusUid();
        protected override void ValueUpdated()
        {
            ClearError();
        }
        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" && e.ShiftKey) // for textarea the shortcut to submit is shift enter
                await OnSubmit.InvokeAsync();
            else if (e.Code == "Escape")
                await OnClose.InvokeAsync();
        }
    }
}