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

    public partial class Libraries : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }
        [Inject] NotificationService NotificationService { get; set; }
        protected RadzenDataGrid<Library> DataGrid { get; set; }

        private List<Library> Data = new List<Library>();

        private IList<Library> SelectedItems;
        private string lblAdd, lblEdit, lblDelete, lblDeleting;

        const string API_URL = "/api/library";
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
                var result = await HttpHelper.Get<List<Library>>(API_URL);
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

        async Task Enable(bool enabled, Library library)
        {
            Blocker.Show();
            this.StateHasChanged();
            Data.Clear();
            try
            {
                await HttpHelper.Put<Library>($"{API_URL}/state/{library.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

        private Library EditingItem = null;
        private async Task Add()
        {
            await Edit(new Library() { Enabled = true });
        }

        async Task Edit()
        {
            var selected = this.SelectedItems?.FirstOrDefault();
            if (selected == null)
                return;
            await Edit(selected);
        }

        async Task Edit(Library library)
        {
            this.EditingItem = library;
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Text,
                Name = nameof(library.Name)
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Folder,
                Name = nameof(library.Path)
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Text,
                Name = nameof(library.Filter)
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Switch,
                Name = nameof(library.Enabled)
            });
            var result = await Editor.Open("Pages.Library", library.Name, fields, library,
              saveCallback: Save);
        }

        async Task<bool> Save(ExpandoObject model)
        {
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var saveResult = await HttpHelper.Post<Library>($"{API_URL}", model);
                if (saveResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, Translater.Instant("ErrorMessages.SaveFailed"));
                    return false;
                }

                int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
                if (index < 0)
                    this.Data.Add(saveResult.Data);
                else
                    this.Data[index] = saveResult.Data;
                await DataGrid.Reload();

                return true;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
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
                var deleteResult = await HttpHelper.Delete($"{API_URL}", new DeleteModel { Uids = uids });
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

        private async Task RowDoubleClicked(DataGridRowMouseEventArgs<Library> item)
        {
            this.SelectedItems.Clear();
            this.SelectedItems.Add(item.Data);
            await Edit(item.Data);
        }
    }

}