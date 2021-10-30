namespace FileFlow.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Radzen;
    using Radzen.Blazor;
    using FileFlow.Client.Components;
    using FileFlow.Client.Helpers;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using FileFlow.Client.Models;

    public partial class Libraries : ListPage<Library>
    {
        public override string ApIUrl => "/api/library";

        private Library EditingItem = null;

        private async Task Add()
        {
            await Edit(new Library() { Enabled = true });
        }


        public override async Task Edit(Library library)
        {
            Blocker.Show();
            var flowResult = await HttpHelper.Get<FileFlow.Shared.Models.Flow[]>("/api/flow");
            Blocker.Hide();
            if (flowResult.Success == false || flowResult.Data?.Any() != true)
            {
                ShowEditHttpError(flowResult, "Pages.Libraries.Error.NoFlows");
                return;
            }
            var flowOptions = flowResult.Data.Select(x => new ListOption { Value = x.Uid, Label = x.Name });


            this.EditingItem = library;
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Text,
                Name = nameof(library.Name),
                Validators = new List<FileFlow.Shared.Validators.Validator> {
                    new FileFlow.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Folder,
                Name = nameof(library.Path),
                Validators = new List<FileFlow.Shared.Validators.Validator> {
                    new FileFlow.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Text,
                Name = nameof(library.Filter)
            });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.Select,
                Name = nameof(library.Flow),
                Parameters = new Dictionary<string, object>{
                    { "Options", flowOptions }
                }
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
                var saveResult = await HttpHelper.Post<Library>($"{ApIUrl}", model);
                if (saveResult.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error, saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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

    }

}