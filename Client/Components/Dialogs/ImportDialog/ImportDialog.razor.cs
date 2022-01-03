namespace FileFlows.Client.Components.Dialogs
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using FileFlows.Shared;
    using Microsoft.JSInterop;
    using Microsoft.AspNetCore.Components.Forms;
    using System.IO;

    public partial class ImportDialog : ComponentBase
    {
        private string lblImport, lblCancel, lblBrowse;
        private string Message, Title;
        TaskCompletionSource<string> ShowTask;

        private string FileName { get; set; }
        private bool HasFile { get; set; }
        private static ImportDialog Instance { get; set; }

        private bool Visible { get; set; }

        private string Value { get; set; }

        private readonly string[] Extensions = new[] { "json" };

        private string Uid = System.Guid.NewGuid().ToString();

        private bool Focus;

        [Inject] private IJSRuntime jsRuntime { get; set; }

        protected override void OnInitialized()
        {
            this.lblImport = Translater.Instant("Labels.Import");
            this.lblCancel = Translater.Instant("Labels.Cancel");
            this.lblBrowse = Translater.Instant("Labels.Browse");
            this.Title = Translater.Instant("Dialogs.Import.Title");
            this.Message = Translater.Instant("Dialogs.Import.Message");
            Instance = this;
        }

        public static Task<string> Show()
        {
            if (Instance == null)
                return Task.FromResult<string>("");

            return Instance.ShowInstance();
        }

        private Task<string> ShowInstance()
        {
            this.Value = string.Empty;
            this.FileName = string.Empty;
            this.Visible = true;
            this.Focus = true;
            this.StateHasChanged();

            Instance.ShowTask = new TaskCompletionSource<string>();
            return Instance.ShowTask.Task;
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

        private async Task LoadFile(InputFileChangeEventArgs e)
        {
            if (e.FileCount == 0)
            {
                FileName = string.Empty;
                Value = string.Empty;
                HasFile = false;
                return;
            }
            FileName = e.File.Name;
            using var reader = new StreamReader(e.File.OpenReadStream());
            this.Value = await reader.ReadToEndAsync();
            this.HasFile = string.IsNullOrWhiteSpace(this.Value) == false;
            this.StateHasChanged();
        }
    }
}