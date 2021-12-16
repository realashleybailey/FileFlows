using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;

namespace FileFlows.Client.Components.Dialogs
{
    public partial class Confirm : ComponentBase
    {
        private string lblYes, lblNo;
        private string Message, Title;
        TaskCompletionSource<bool> ShowTask;

        private static Confirm Instance { get; set; }

        private bool Visible { get; set; }

        protected override void OnInitialized()
        {
            this.lblYes = Translater.Instant("Labels.Yes");
            this.lblNo = Translater.Instant("Labels.No");
            Instance = this;
        }

        public static Task<bool> Show(string title, string message)
        {
            if (Instance == null)
                return Task.FromResult<bool>(false);

            return Instance.ShowInstance(title, message);
        }

        private Task<bool> ShowInstance(string title, string message)
        {
            this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Confirm");
            this.Message = Translater.TranslateIfNeeded(message ?? "");
            this.Visible = true;
            this.StateHasChanged();

            Instance.ShowTask = new TaskCompletionSource<bool>();
            return Instance.ShowTask.Task;
        }

        private async void Yes()
        {
            this.Visible = false;
            Instance.ShowTask.TrySetResult(true);
            await Task.CompletedTask;
        }

        private async void No()
        {
            this.Visible = false;
            Instance.ShowTask.TrySetResult(false);
            await Task.CompletedTask;
        }
    }
}