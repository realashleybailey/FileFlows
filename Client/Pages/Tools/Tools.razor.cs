namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;
    using Radzen;
    using Radzen.Blazor;
    using FileFlows.Client.Components;
    using FileFlows.Client.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using FileFlows.Plugin;

    public partial class Tools : ListPage<Tool>
    {
        public override string ApIUrl => "/api/tool";

        private Tool EditingItem = null;

        private async Task Add()
        {
            await Edit(new Tool());
        }


        public override async Task Edit(Tool Tool)
        {
            this.EditingItem = Tool;
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FileFlows.Plugin.FormInputType.Text,
                Name = nameof(Tool.Name),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FileFlows.Plugin.FormInputType.File,
                Name = nameof(Tool.Path),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            var result = await Editor.Open("Pages.Tool", Tool.Name, fields, Tool,
              saveCallback: Save);
        }

        async Task<bool> Save(ExpandoObject model)
        {
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var saveResult = await HttpHelper.Post<Tool>($"{ApIUrl}", model);
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