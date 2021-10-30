namespace FileFlow.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Radzen.Blazor;
    using FileFlow.Client.Components;
    using FileFlow.Client.Helpers;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using Radzen;
    using System.Linq;
    using FileFlow.Client.Components.Dialogs;

    public partial class LibraryFiles : ListPage<LibraryFile>
    {
        public override string ApIUrl => "/api/library-file";
        private string lblIgnore, lblProcess, lblView;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblIgnore = Translater.Instant("Pages.LibraryFiles.Button.Ignore");
            lblProcess = Translater.Instant("Pages.LibraryFiles.Button.Process");
            lblView = Translater.Instant("Pages.LibraryFiles.Button.View");
        }


        public override async Task Edit(LibraryFile item)
        {
            Blocker.Show();
            var result = await HttpHelper.Get<LibraryFile>(ApIUrl + "/" + item.Uid);
            Blocker.Hide();
            if (result.Success == false)
            {
                ShowEditHttpError(result);
                return;
            }

            item = result.Data;

            List<ElementField> fields = new List<ElementField>();
            // fields.Add(new ElementField
            // {
            //     InputType = FileFlow.Plugin.FormInputType.Text,
            //     Name = nameof(item.Name),
            //     Validators = new List<FileFlow.Shared.Validators.Validator> {
            //         new FileFlow.Shared.Validators.Required()
            //     }
            // });
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.LogView,
                Name = nameof(item.Log)
            });

            await Editor.Open("Pages.LibraryFile", item.Name, fields, item, large: true, readOnly: true);
        }


    }
}