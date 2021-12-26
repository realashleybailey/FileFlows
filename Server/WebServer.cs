using FileFlows.Node.Workers;
using FileFlows.Server.Workers;

namespace FileFlows.Server
{
    public class WebServer
    {
        private static WebApplication app;

        public static async Task Stop()
        {
            if (app == null)
                return;
            await app.StopAsync();
        }

        public static void Start(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();

            app = builder.Build();
            
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
            Logger.Instance.ILog(new string('=', 50));

            Helpers.TranslaterHelper.InitTranslater();

            Shared.Helpers.HttpHelper.Client = new HttpClient();

            using var pl = new ServerShared.Helpers.PluginHelper();
            pl.ScanForPlugins();

            LibraryWorker.ResetProcessing();
            WorkerManager.StartWorkers(
                new LibraryWorker(),
                new FlowWorker(isServer: true),
                new TelemetryReporter()
            );

            app.MapHub<Hubs.FlowHub>("/flow");

            // this will run the asp.net app and wait until it is killed
            Console.WriteLine("Running FileFlows Server");
            app.Run();
            Console.WriteLine("Finished running FileFlows Server");

            WorkerManager.StopWorkers();
        }
    }
}
