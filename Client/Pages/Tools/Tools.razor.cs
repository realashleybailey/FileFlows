namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Threading.Tasks;
    using Radzen;
    using FileFlows.Client.Components;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;

    public partial class Tools : ListPage<Tool>
    {
        public override string ApiUrl => "/api/tool";

        private Tool EditingItem = null;

        private async Task Add()
        {
#if (!DEMO)
            await Edit(new Tool());
#endif
        }


        public override async Task<bool> Edit(Tool Tool)
        {
#if (!DEMO)
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
            var result = await Editor.Open("Pages.Tool", "Pages.Tool.Title", fields, Tool,
              saveCallback: Save);
#endif
            return false;
        }

        async Task<bool> Save(ExpandoObject model)
        {
#if (DEMO)
            return true;
#else
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var saveResult = await HttpHelper.Post<Tool>($"{ApiUrl}", model);
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
                await this.Load(saveResult.Data.Uid);

                return true;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

    }
}