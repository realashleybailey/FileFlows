namespace FileFlow.Client.Components 
{
    using Microsoft.AspNetCore.Components;
    using FileFlow.Shared;

    public partial class Blocker : ComponentBase
    {

        public bool Visible { get; set; }

        public string Message { get; set; } = "";

        public void Show(string message = "")
        {
            // nulls are yucky
            Message ??= "";
            message ??= "";

            if (Translater.NeedsTranslating(message))
                message = Translater.Instant(message);

            if (this.Visible == true && Message == message)
                return;

            this.Visible = true;
            this.Message = message;
            this.StateHasChanged();
        }

        public void Hide()
        {
            if (this.Visible == false)
                return;
            this.Message = "";
            this.Visible = false;
            this.StateHasChanged();
        }

    }
}