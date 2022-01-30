using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using System.Runtime.InteropServices;

namespace FileFlows.Server
{
    public class WebServer
    {
        private static WebApplication app;
        public static int Port { get; private set; }
        public static async Task Stop()
        {
            if (app == null)
                return;
            await app.StopAsync();
        }

        public static void Start(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Port = 5000;
#if (DEBUG)
            Port = 6868;
#endif
            string url = args?.Where(x => x.StartsWith("--urls=")).FirstOrDefault();
            if(string.IsNullOrEmpty(url) == false)
            {
                var portMatch = Regex.Match(url, @"(?<=(:))[\d]+");
                if (portMatch.Success)
                    Port = int.Parse(portMatch.Value);
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();
            builder.Services.AddResponseCompression();

            builder.Services.AddMvc();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileFlows", Version = "v1" });

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.Server.xml");
                if (File.Exists(filePath))
                    c.IncludeXmlComments(filePath);
                else
                {
                    filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.xml");
                    if (File.Exists(filePath))
                        c.IncludeXmlComments(filePath);
                }
            });

            app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.IndexStream = () => typeof(WebServer).Assembly.GetManifestResourceStream("FileFlows.Server.Resources.SwaggerIndex.html");

                c.RoutePrefix = "api/help";
                c.DocumentTitle = "FileFlows API";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileFlows API");
                c.InjectStylesheet("/css/swagger.min.css");
            });

            app.UseDefaultFiles();

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            provider.Mappings[".br"] = "text/plain";

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider,
                OnPrepareResponse = x =>
                {
                    if (x?.File?.PhysicalPath?.ToLower()?.Contains("_framework") == true)
                        return;
                    if (x?.File?.PhysicalPath?.ToLower()?.Contains("_content") == true)
                        return;
                    x?.Context?.Response?.Headers?.Append("Cache-Control", "no-cache");
                }
            });

            app.UseMiddleware<ExceptionMiddleware>();
            app.UseRouting();


            Globals.IsDevelopment = app.Environment.IsDevelopment();
            Globals.IsWindows = isWindows;

            if (Globals.IsDevelopment)
                app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.MapControllerRoute(
                 name: "default",
                 pattern: "{controller=Home}/{action=Index}/{id?}");

            // this will allow refreshing from a SPA url to load the index.html file
            app.MapControllerRoute(
                name: "Spa",
                pattern: "{*url}",
                defaults: new { controller = "Home", action = "Spa" }
            );

            Shared.Logger.Instance = Logger.Instance;

            Services.InitServices.Init();

            //if (FileFlows.Server.Globals.IsDevelopment == false)
            //    FileFlows.Server.Helpers.DbHelper.StartMySqlServer();
            Helpers.DbHelper.CreateDatabase().Wait();


            // do this so the settings object is loaded, and the time zone is set
            new Controllers.SettingsController().Get().Wait();

            Logger.Instance.ILog(new string('=', 50));
            Logger.Instance.ILog("Starting File Flows " + Globals.Version);
            if(Program.Docker)
                Logger.Instance.ILog("Running inside docker container");

            Logger.Instance.ILog(new string('=', 50));

            StartupCleanup();


            // need to scan for plugins before initing the translater as that depends on the plugins directory
            Helpers.PluginScanner.Scan();

            Helpers.TranslaterHelper.InitTranslater();

            Shared.Helpers.HttpHelper.Client = new HttpClient();

            ServerShared.Services.Service.ServiceBaseUrl = $"http://localhost:{Port}";


            LibraryWorker.ResetProcessing();
            WorkerManager.StartWorkers(
                new LibraryWorker(),
                new FlowWorker(string.Empty, isServer: true),
                new PluginUpdaterWorker(),
                new TelemetryReporter(),
                isWindows ? new AutoUpdater() : null
            );

            app.MapHub<Hubs.FlowHub>("/flow");

            app.UseResponseCompression();

            // this will run the asp.net app and wait until it is killed
            Console.WriteLine("Running FileFlows Server");

            app.Run($"http://0.0.0.0:{Port}/");                
            
            Console.WriteLine("Finished running FileFlows Server");

            WorkerManager.StopWorkers();
        }
        private static void StartupCleanup()
        {
            try
            {
                if (Program.Docker)
                    return;

                Logger.Instance.ILog("Startup cleanup");
                Workers.AutoUpdater.CleanUpOldFiles(60_000);

                string? source = new Controllers.SettingsController().Get().Result?.LoggingPath;
                if (string.IsNullOrEmpty(source))
                    return;

                string dest = Path.Combine(source, "LibraryFiles");
                if (Directory.Exists(dest) == false)
                {
                    Logger.Instance.ILog("Creating LibraryFiles Logging: " + dest);
                    Directory.CreateDirectory(dest);
                }

                foreach (var file in new DirectoryInfo(source).GetFiles("*.log"))
                {
                    if (file.Name.Contains("FileFlows"))
                        continue;
                    if (file.Name.Length != 40)
                    {
                        Logger.Instance.ILog("Not a library file: " + file.Name);
                        continue; // not a guid name
                    }
                    try
                    {
                        file.MoveTo(Path.Combine(dest, file.Name), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed moving file: " + file.Name + " => " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("Failed moving old log files: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
