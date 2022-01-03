namespace FileFlows.Client.Helpers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FileFlows.Client.Components;
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public class LibraryFileEditor
    {
        static string ApIUrl => "/api/library-file";

        private static async Task<RequestResult<LibraryFileModel>> GetLibraryFile(string url)
        {
#if (DEMO)
            var file = new LibraryFileModel
            {
                Name = "Demo Library File.mkv"
            };
            return new RequestResult<LibraryFileModel> { Success = true, Data = file };
#else
            return await HttpHelper.Get<LibraryFileModel>(url);
#endif
        }
        private static async Task<RequestResult<string>> GetLibraryFileLog(string url)
        {
#if (DEMO)
            return new RequestResult<string> { Success = true, Data = "This is a sample log file." };
#else
            return await HttpHelper.Get<string>(url);
#endif
        }

        public static async Task Open(Blocker blocker, Editor editor, LibraryFile item)
        {
            LibraryFileModel model = null;
            string logUrl = ApIUrl + "/" + item.Uid + "/log";
            blocker.Show();
            try
            {
                var result = await GetLibraryFile(ApIUrl + "/" + item.Uid);
                if (result.Success == false)
                {
                    Toast.ShowError(
                        result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant("ErrorMessage.NotFound") : Translater.TranslateIfNeeded(result.Body),
                        duration: 60_000
                    );
                    return;
                }

                model = result.Data;
                var logResult = await GetLibraryFileLog(logUrl);
                model.Log = (logResult.Success ? logResult.Data : string.Empty) ?? string.Empty;

            }
            finally
            {

                blocker.Hide();
            }

            List<ElementField> fields = new List<ElementField>();

            bool processing = model.Status == FileStatus.Processing;
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