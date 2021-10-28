namespace FileFlow.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen.Blazor;
    using FileFlow.Client.Components;
    using FileFlow.Client.Helpers;
    using FileFlow.Shared;
    using xVideoFile = FileFlow.Shared.Models.VideoFile;

    public partial class VideoFiles : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        private string lblScanning, lblScan, lblIgnore, lblProcess, lblView;
        IEnumerable<int> pageSizeOptions = new int[] { 50, 100, 250, 500 };

        private RadzenDataGrid<xVideoFile> _DataGrid;
        protected RadzenDataGrid<xVideoFile> DataGrid
        {
            get => _DataGrid;
            set
            {
                if (_DataGrid == null && value != null)
                    value.PageSize = 250;
                _DataGrid = value;
            }
        }

        private bool IsScanning { get; set; }
        protected override void OnInitialized()
        {
            lblScan = Translater.Instant("Pages.VideoFiles.Button.Scan");
            lblScanning = Translater.Instant("Pages.VideoFiles.Button.Scanning");
            lblIgnore = Translater.Instant("Pages.VideoFiles.Button.Ignore");
            lblProcess = Translater.Instant("Pages.VideoFiles.Button.Process");
            lblView = Translater.Instant("Pages.VideoFiles.Button.View");
        }

        private List<xVideoFile> Files = new List<xVideoFile>();

        private IList<xVideoFile> SelectedItems;

        async Task Scan()
        {
            IsScanning = true;
            Blocker.Show(lblScanning);
            Files.Clear();
            try
            {
                var files = await HttpHelper.Get<List<FileFlow.Shared.Models.VideoFile>>("/api/video-file/scan");
                if (files.Success)
                {
                    this.Files = files.Data;
                }
            }
            finally
            {
                IsScanning = false;
                Blocker.Hide();
            }
        }
    }
}