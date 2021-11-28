namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen;
    using Radzen.Blazor;
    using FileFlows.Client.Components;
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using ffFlow = FileFlows.Shared.Models.Flow;
    using System;

    public partial class Flows : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [CascadingParameter] Blocker Blocker { get; set; }
        [Inject] NotificationService NotificationService { get; set; }
        protected RadzenDataGrid<ffFlow> DataGrid { get; set; }

        private List<ffFlow> Data = new List<ffFlow>();

        private IList<ffFlow> SelectedItems;
        private string lblAdd, lblEdit, lblDelete, lblDeleting;

        const string API_URL = "/api/flow";
        protected override void OnInitialized()
        {
            lblAdd = Translater.Instant("Labels.Add");
            lblEdit = Translater.Instant("Labels.Edit");
            lblDelete = Translater.Instant("Labels.Delete");
            lblDeleting = Translater.Instant("Labels.Deleting");
            _ = Load();
        }

        async Task Load()
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                var result = await GetData(API_URL);
                if (result.Success)
                {
                    this.Data = result.Data;
                }
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        private async Task<RequestResult<List<ffFlow>>> GetData(string url)
        {
#if (DEMO)
            var results = Enumerable.Range(1, 10).Select(x => new ffFlow
            {
                Uid = Guid.NewGuid(),
                Name = "Demo Flow " + x,
                Enabled = x < 5
            }).ToList();
            return new RequestResult<List<ffFlow>> { Success = true, Data = results };
#endif
            return await HttpHelper.Get<List<ffFlow>>(url);
        }

        async Task Enable(bool enabled, ffFlow flow)
        {
#if (DEMO)
            return;
#else
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                await HttpHelper.Put<ffFlow>($"{API_URL}/state/{flow.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

        private void Add()
        {
            NavigationManager.NavigateTo("flows/" + Guid.Empty);
        }

        private void Edit()
        {
            var item = this.SelectedItems?.FirstOrDefault();
            if (item != null)
                NavigationManager.NavigateTo("flows/" + item.Uid);
        }
        private async Task RowDoubleClicked(DataGridRowMouseEventArgs<ffFlow> item)
        {
            this.SelectedItems.Clear();
            this.SelectedItems.Add(item.Data);
            Edit();
            await Task.CompletedTask;
        }

        async Task Delete()
        {
            var uids = this.SelectedItems?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
            if (uids.Length == 0)
                return; // nothing to delete
            if (await Confirm.Show("Labels.Delete",
                Translater.Instant("Labels.DeleteItems", new { count = uids.Length })) == false)
                return; // rejected the confirm

            Blocker.Show();
            this.StateHasChanged();

            try
            {
#if (!DEMO)
                var deleteResult = await HttpHelper.Delete($"{API_URL}", new ReferenceModel { Uids = uids });
                if (deleteResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("ErrorMessages.DeleteFailed"));
                    return;
                }
#endif

                this.SelectedItems.Clear();
                this.Data.RemoveAll(x => uids.Contains(x.Uid));
                await DataGrid.Reload();
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }
    }

}