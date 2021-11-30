namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FileFlows.Client.Components;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Linq;
    using System;
    using Microsoft.AspNetCore.Components.Web;

    public partial class LibraryFiles : ListPage<LibraryFile>
    {
        public override string ApIUrl => "/api/library-file";

        private FileFlows.Shared.Models.FileStatus SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;

        private string lblMoveToTop = "";

        private readonly List<LibraryStatus> Statuses = new List<LibraryStatus>();

        private void SetSelected(LibraryStatus status)
        {
            SelectedStatus = status.Status;
            this.StateHasChanged();
            _ = this.Refresh();
        }


#if (DEMO)
        public override async Task Load()
        {
            this.Data = Enumerable.Range(1, SelectedStatus == FileStatus.Processing ? 1 : 10).Select(x => new LibraryFile
            {
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now,
                Flow = new ObjectReference
                {
                    Name = "Flow",
                    Uid = Guid.NewGuid()
                },
                Library = new ObjectReference
                {
                    Name = "Library",
                    Uid = Guid.NewGuid(),
                },
                Name = "File_" + x + ".ext",
                RelativePath = "File_" + x + ".ext",
                Uid = Guid.NewGuid(),
                Status = SelectedStatus,
                OutputPath = SelectedStatus == FileStatus.Processed ? "output/File_" + x + ".ext" : string.Empty
            }).ToList();

            await PostLoad();
        }
#endif

        public override string FetchUrl => ApIUrl + "?status=" + SelectedStatus;

        public override async Task PostLoad()
        {
            await RefreshStatus();
        }
        protected override async Task PostDelete()
        {
            await RefreshStatus();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblMoveToTop = Translater.Instant("Pages.LibraryFiles.Buttons.MoveToTop");
        }

        private async Task<RequestResult<List<LibraryStatus>>> GetStatus()
        {
#if (DEMO)

            var results = new List<LibraryStatus>
            {
                new LibraryStatus { Status = FileStatus.Unprocessed, Count = 10 },
                new LibraryStatus { Status = FileStatus.Processing, Count = 1 },
                new LibraryStatus { Status = FileStatus.Processed, Count = 10 },
                new LibraryStatus { Status = FileStatus.ProcessingFailed, Count = 10 }
            };
            return new RequestResult<List<LibraryStatus>> { Success = true, Data = results };
#endif
            return await HttpHelper.Get<List<LibraryStatus>>(ApIUrl + "/status");
        }

        private async Task RefreshStatus()
        {
            var result = await GetStatus();
            if (result.Success)
            {
                var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
                foreach (var s in order)
                {
                    if (result.Data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
                        result.Data.Add(new LibraryStatus { Status = s });

                }
                foreach (var s in result.Data)
                    s.Name = Translater.Instant("Enums.FileStatus." + s.Status.ToString());
                Statuses.Clear();
                Statuses.AddRange(result.Data.OrderBy(x => { int index = order.IndexOf(x.Status); return index >= 0 ? index : 100; }));
            }
        }

        public override async Task Edit(LibraryFile item)
        {
            await Helpers.LibraryFileEditor.Open(Blocker, NotificationService, Editor, item);
        }

        public async Task MoveToTop()
        {
#if (DEMO)
            return;
#else

            var uids = this.SelectedItems?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
            if (uids.Length == 0)
                return; // nothing to delete

            Blocker.Show();
            try
            {
                await HttpHelper.Post(ApIUrl + "/move-to-top", new ReferenceModel { Uids = uids });
                this.SelectedItems.Clear();
            }
            finally
            {
                Blocker.Hide();
            }
            await Refresh();
#endif
        }

        private void FilterKeyDown(KeyboardEventArgs e)
        {
            if(e.Key == "Enter")
            {
                UpdateFilter();
            }
        }
    }
}