namespace FileFlows.Client.Components.Dialogs
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using FileFlows.Shared;
    using Microsoft.JSInterop;

    public partial class Prompt : ComponentBase
    {
        private string lblOk, lblCancel;
        private string Message, Title;
        TaskCompletionSource<string> ShowTask;

        private static Prompt Instance { get; set; }

        private bool Visible { get; set; }

        private string Value { get; set; }

        private string Uid = System.Guid.NewGuid().ToString();

        private bool Focus;

        [Inject] private IJSRuntime jsRuntime { get; set; }

        protected override void OnInitialized()
        {
            this.lblOk = Translater.Instant("Labels.Ok");
            this.lblCancel = Translater.Instant("Labels.Cancel");
            Instance = this;
        }

        public static Task<string> Show(string title, string message, string value = "")
        {
            if (Instance == null)
                return Task.FromResult<string>("");

            return Instance.ShowInstance(title, message, value);
        }

        private Task<string> ShowInstance(string title, string message, string value = "")
        {
            this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Prompt");
            this.Message = Translater.TranslateIfNeeded(message ?? "");
            this.Value = value ?? "";
            this.Visible = true;
            this.Focus = true;
            this.StateHasChanged();

            Instance.ShowTask = new TaskCompletionSource<string>();
            return Instance.ShowTask.Task;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Focus)
            {
                Focus = false;
                await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
            }
        }

        private void OnKeyDown(KeyboardEventArgs e)
        {
            if (e.AltKey || e.CtrlKey || e.ShiftKey || string.IsNullOrWhiteSpace(Value))
                return;
            if (e.Key == "Enter")
            {
                Accept();
            }
        }

        private async void Accept()
        {
            this.Visible = false;
            Instance.ShowTask.TrySetResult(Value);
            await Task.CompletedTask;
        }

        private async void Cancel()
        {
            this.Visible = false;
            Instance.ShowTask.TrySetResult("");
            await Task.CompletedTask;
        }
    }
}