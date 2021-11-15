namespace FileFlow.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen;
    using Radzen.Blazor;
    using FileFlow.Client.Components;
    using FileFlow.Client.Components.Dialogs;
    using FileFlow.Client.Helpers;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using ffFlow = FileFlow.Shared.Models.Flow;

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
                var result = await HttpHelper.Get<List<ffFlow>>(API_URL);
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

        async Task Enable(bool enabled, ffFlow flow)
        {
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
        }

        private void Add()
        {
            NavigationManager.NavigateTo("flows/" + System.Guid.Empty);
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
                var deleteResult = await HttpHelper.Delete($"{API_URL}", new ReferenceModel { Uids = uids });
                if (deleteResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("ErrorMessages.DeleteFailed"));
                    return;
                }

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