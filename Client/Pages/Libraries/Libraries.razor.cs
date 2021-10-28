namespace ViWatcher.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen.Blazor;
    using ViWatcher.Client.Components;
    using ViWatcher.Client.Helpers;
    using ViWatcher.Shared;
    using ViWatcher.Shared.Models;

    public partial class Libraries : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [CascadingParameter] Editor Editor { get; set; }
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
            var newItem = await Edit(new Library());
            if (newItem.Uid != System.Guid.Empty)
                this.Data.Add(newItem);
        }

        async Task<Library> Edit(Library library)
        {
            this.EditingItem = library;
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = ViWatcher.Plugins.FormInputType.Text,
                Name = nameof(library.Name)
            });
            fields.Add(new ElementField
            {
                InputType = ViWatcher.Plugins.FormInputType.Text,
                Name = nameof(library.Path)
            });
            fields.Add(new ElementField
            {
                InputType = ViWatcher.Plugins.FormInputType.Switch,
                Name = nameof(library.Enabled)
            });
            Logger.Instance.DLog("About to open eeditor");
            var result = await Editor.Open("Pages.Library", library.Name, fields, library,
              saveCallback: Save);
            Logger.Instance.DLog("finshed with eeditor");

            string json = System.Text.Json.JsonSerializer.Serialize(result);
            var updated = System.Text.Json.JsonSerializer.Deserialize<Library>(json);
            return updated;
        }

        async Task<bool> Save(ExpandoObject model)
        {
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var saveResult = await HttpHelper.Post<Library>($"{API_URL}", model);
                if (saveResult.Success == false)
                    return false;
                return true;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        }

    }

}