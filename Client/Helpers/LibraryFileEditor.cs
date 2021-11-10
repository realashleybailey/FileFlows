namespace FileFlow.Client.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FileFlow.Client.Components;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;
    using Radzen;

    public class LibraryFileEditor
    {
        static string ApIUrl => "/api/library-file";
        public static async Task Open(Blocker blocker, NotificationService notificationService, Editor editor, LibraryFile item)
        {
            blocker.Show();
            var result = await HttpHelper.Get<LibraryFile>(ApIUrl + "/" + item.Uid);
            blocker.Hide();
            if (result.Success == false)
            {
                notificationService.Notify(NotificationSeverity.Error,
                    result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant("ErrorMessage.NotFound") : Translater.TranslateIfNeeded(result.Body),
                    duration: 60_000
                );
                return;
            }

            item = result.Data;

            List<ElementField> fields = new List<ElementField>();

            bool processing = item.Status == FileStatus.Processing;
            Logger.Instance.DLog("Item is processing: " + processing);
            fields.Add(new ElementField
            {
                InputType = FileFlow.Plugin.FormInputType.LogView,
                Name = nameof(item.Log),
                Parameters = processing ? new Dictionary<string, object> {
                    { nameof(Components.Inputs.InputLogView.RefreshUrl), ApIUrl + "/" + item.Uid + "/log" },
                    { nameof(Components.Inputs.InputLogView.RefreshSeconds), 5 },
                } : null
            });

            await editor.Open("Pages.LibraryFile", item.Name, fields, item, large: true, readOnly: true);
        }
    }
}