using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

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
            });

            app = builder.Build();

            app.UseSwagger(c =>
            {
                //c.SerializeAsV2 = true;
            });
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "api/help";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileFlows API");
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
                Logger.Instance?.ILog("Running inside docker container");
            Logger.Instance.ILog(new string('=', 50));

            // need to scan for plugins before initing the translater as that depends on the plugins directory
            Helpers.PluginScanner.Scan();

            Helpers.TranslaterHelper.InitTranslater();

            Shared.Helpers.HttpHelper.Client = new HttpClient();

            ServerShared.Services.Service.ServiceBaseUrl = $"http://localhost:{Port}";


            LibraryWorker.ResetProcessing();
            WorkerManager.StartWorkers(
                new LibraryWorker(),
                new FlowWorker(isServer: true),
                new PluginUpdaterWorker(),
                new TelemetryReporter()
            );

            app.MapHub<Hubs.FlowHub>("/flow");

            app.UseResponseCompression();

            // this will run the asp.net app and wait until it is killed
            Console.WriteLine("Running FileFlows Server");
            //app.Run($"http://[::]:{port}/");
            app.Run();
            Console.WriteLine("Finished running FileFlows Server");

            WorkerManager.StopWorkers();
        }
    }
}
