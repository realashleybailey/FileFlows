namespace FileFlow.Client.Components.Inputs
{
    using System.Threading.Tasks;
    using FileFlow.Client.Components.Dialogs;
    using Microsoft.AspNetCore.Components;
    public partial class InputFile : Input<string>
    {
        [Parameter]
        public string[] Extensions { get; set; }

        [Parameter]
        public bool Directory { get; set; }

        async Task Browse()
        {
            string result = await FileBrowser.Show(this.Value, directory: Directory, extensions: Extensions);
            if (string.IsNullOrEmpty(result))
                return;
            this.ClearError();
            this.Value = result;
        }
    }
}