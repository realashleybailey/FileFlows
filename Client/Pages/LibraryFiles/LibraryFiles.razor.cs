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
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Plugin;

    public partial class LibraryFiles : ListPage<LibraryFile>
    {
        public override string ApiUrl => "/api/library-file";

        private FileFlows.Shared.Models.FileStatus SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;

        private string lblMoveToTop = "";

        private readonly List<LibraryStatus> Statuses = new List<LibraryStatus>();

        private int Count;

        private string Title;
        private string lblLibraryFiles, lblFileFlowsServer;

        private void SetSelected(LibraryStatus status)
        {
            SelectedStatus = status.Status;
            Title = lblLibraryFiles + ": " + status.Name;
            _ = this.Refresh();
        }

        //protected virtual Task<RequestResult<List<LibraryFile>>> FetchData()
        //{
        //    return HttpHelper.Get<List<LibraryFile>>($"{FetchUrl}?skip=0&top=250");
        //}


#if (DEMO)
        public override async Task Load(Guid? selectedUid = null)
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

        public override string FetchUrl => ApiUrl + "?status=" + SelectedStatus;

        private string NameMinWidth = "20ch";

        public override async Task PostLoad()
        {
            if(App.Instance.IsMobile)
                this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length / 2) ?? 0))) + "ch" : "20ch";
            else
                this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length) ?? 0))) + "ch" : "20ch";
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
            lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
            lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
            Title = lblLibraryFiles + ": " + Translater.Instant("Enums.FileStatus." + FileStatus.Unprocessed);

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
            return await HttpHelper.Get<List<LibraryStatus>>(ApiUrl + "/status");
        }

        private async Task RefreshStatus()
        {
            var result = await GetStatus();
            if (result.Success)
            {
                var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
                foreach (var s in order)
                {
                    if (result.Data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
                        result.Data.Add(new LibraryStatus { Status = s });

                }
                foreach (var s in result.Data)
                    s.Name = Translater.Instant("Enums.FileStatus." + s.Status.ToString());
                Statuses.Clear();
                Statuses.AddRange(result.Data.OrderBy(x => { int index = order.IndexOf(x.Status); return index >= 0 ? index : 100; }));

                this.Count = Statuses.Where(x => x.Status == SelectedStatus).Select(x => x.Count).FirstOrDefault();
            }
        }

        public override async Task<bool> Edit(LibraryFile item)
        {
            await Helpers.LibraryFileEditor.Open(Blocker, Editor, item);
            return false;
        }

        public async Task MoveToTop()
        {
#if (DEMO)
            return;
#else

            var selected = Table.GetSelected();
            var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
            if (uids.Length == 0)
                return; // nothing to delete

            Blocker.Show();
            try
            {
                await HttpHelper.Post(ApiUrl + "/move-to-top", new ReferenceModel { Uids = uids });                
            }
            finally
            {
                Blocker.Hide();
            }
            await Refresh();
#endif
        }


        public async Task Cancel()
        {
#if (DEMO)
            return;
#else
            var selected = Table.GetSelected().ToArray();
            if (selected.Length == 0)
                return; // nothing to cancel

            if (await Confirm.Show("Labels.Cancel",
                Translater.Instant("Labels.CancelItems", new { count = selected.Length })) == false)
                return; // rejected the confirm

            Blocker.Show();
            this.StateHasChanged();
            try
            {
                foreach(var item in selected)
                    await HttpHelper.Delete($"/api/worker/by-file/{item.Uid}");

            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
            await Refresh();
#endif
        }

        public async Task Reprocess()
        {
#if (DEMO)
            return;
#else

            var selected = Table.GetSelected();
            var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
            if (uids.Length == 0)
                return; // nothing to reprocess

            Blocker.Show();
            try
            {
                await HttpHelper.Post(ApiUrl + "/reprocess", new ReferenceModel { Uids = uids });
            }
            finally
            {
                Blocker.Hide();
            }
            await Refresh();
#endif
        }
    }
}