using FileFlows.Shared;

namespace FileFlows.Server.Helpers
{
    public class TranslaterHelper
    {
        internal static void InitTranslater(string langCode = "en")
        {
            string appdir = Program.GetAppDirectory();
            string wwwroot = Path.Combine(appdir, $"wwwroot", "i18n", $"{langCode}.json");
            List<string> json = new List<string>();
            if (File.Exists(wwwroot))
                json.Add(File.ReadAllText(wwwroot));
            else
                Logger.Instance.ILog("Language file not found: " + wwwroot);

            foreach (var file in new DirectoryInfo(Path.Combine(appdir, "Plugins"))
                                        .GetFiles($"*{langCode}.json", SearchOption.AllDirectories)
                                        .OrderByDescending(x => x.CreationTime)
                                        .DistinctBy(x => x.Name))
            {
                json.Add(File.ReadAllText(file.FullName));
            }
            Translater.Logger = Logger.Instance;
            Translater.Init(json.ToArray());
        }
    }
}
