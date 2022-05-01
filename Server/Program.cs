using Avalonia;
using FileFlows.Server.Ui;

namespace FileFlows.Server;

public class Program
{
    public static bool Docker { get; private set; }
    internal static bool WindowsGui { get; private set; }

    public static void Main(string[] args)
    {
        try
        {
            Docker = args?.Any(x => x == "--docker") == true;
            InitEncryptionKey();

            if (Docker)
            {
                Console.WriteLine("Starting FileFlows Server...");
                WebServer.Start(args);
            }
            else
            {
                if(Globals.IsWindows)
                    Utils.WindowsConsoleManager.Hide();
                
                Task.Run(() =>
                {
                    Console.WriteLine("Starting FileFlows Server...");
                    WebServer.Start(args);
                });

                try
                {
                    var appBuilder = BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception) { }
            }

            _ = WebServer.Stop();
            Console.WriteLine("Exiting FileFlows Server...");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Instance.ELog("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            catch (Exception) { }
            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    /// <summary>
    /// Init the encryption key, by making this a file in app data, it wont be included with the database if provided to for support
    /// It wont be lost if inside a docker container and updated
    /// </summary>
    private static void InitEncryptionKey()
    {
        string encryptionFile = Path.Combine(GetAppDirectory(), "encryptionkey.txt");
        if (File.Exists(encryptionFile))
        {
            string key = File.ReadAllText(encryptionFile);
            if (string.IsNullOrEmpty(key) == false)
            {
                Helpers.Decrypter.EncryptionKey = key;
                return;
            }
        }
        else
        {
            string key = Guid.NewGuid().ToString();
            File.WriteAllText(encryptionFile, key);
            Helpers.Decrypter.EncryptionKey = key;
        }
    }

    internal static string GetAppDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        if (Docker)
        {
            // docker we move this to the Data directory which is configured outside of the docker image
            // this is so the database and any plugins that are downloaded will be kept if the docker
            // image is updated/re-downloaded.
            dir = Path.Combine(dir, "Data");
        }
        return dir;
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect();
}
