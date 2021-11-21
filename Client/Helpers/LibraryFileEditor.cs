namespace FileFlows.Client.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FileFlows.Client.Components;
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;
    using Radzen;

    public class LibraryFileEditor
    {
        static string ApIUrl => "/api/library-file";
        public static async Task Open(Blocker blocker, NotificationService notificationService, Editor editor, LibraryFile item)
        {
            LibraryFileModel model = null;
            string logUrl = ApIUrl + "/" + item.Uid + "/log";
            blocker.Show();
            try
            {
                var result = await HttpHelper.Get<LibraryFileModel>(ApIUrl + "/" + item.Uid);
                if (result.Success == false)
                {
                    notificationService.Notify(NotificationSeverity.Error,
                        result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant("ErrorMessage.NotFound") : Translater.TranslateIfNeeded(result.Body),
                        duration: 60_000
                    );
                    return;
                }

                model = result.Data;
                var logResult = await HttpHelper.Get<string>(logUrl);
                model.Log = (logResult.Success ? logResult.Data : string.Empty) ?? string.Empty;

            }
            finally
            {

                blocker.Hide();
            }

            List<ElementField> fields = new List<ElementField>();

            bool processing = model.Status == FileStatus.Processing;
            Logger.Instance.DLog("Item is processing: " + processing);
            fields.Add(new ElementField
            {
                InputType = FileFlows.Plugin.FormInputType.LogView,
                Name = "Log",
                Parameters = processing ? new Dictionary<string, object> {
                    { nameof(Components.Inputs.InputLogView.RefreshUrl), logUrl },
                    { nameof(Components.Inputs.InputLogView.RefreshSeconds), 5 },
                } : null
            });

            await editor.Open("Pages.LibraryFile", model.Name, fields, model, large: true, readOnly: true);
        }
    }


    public class LibraryFileModel : LibraryFile
    {
        public string Log { get; set; }
    }
}