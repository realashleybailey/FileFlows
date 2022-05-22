using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FileFlows.Server;
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
        var url = args?.Where(x => x?.StartsWith("--urls=") == true)?.FirstOrDefault();
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
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault | System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

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

        //var wwwroot = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot");
        app.UseStaticFiles(new StaticFileOptions
        {
            //FileProvider = new PhysicalFileProvider(wwwroot),
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
            defaults: new { controller = "Home", action = "Index" }
        );

        Shared.Logger.Instance = Logger.Instance;

        Services.InitServices.Init();

        if (Helpers.DbHelper.Initialize().Result == false)
        {
            Logger.Instance.ELog("Failed initializing database");
            return;
        }
#if(DEBUG)
        //Helpers.DbHelper.CleanDatabase().Wait();
#endif

        // do this so the settings object is loaded
        var settings = new Controllers.SettingsController().Get().Result;

        // run any upgrade code that may need to be run
        new Upgrade.Upgrader().Run(settings);

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
            new LogPruner(),
            new LogConverter(),
            new TelemetryReporter(),
            new ServerUpdater(),
            new FlowRunnerMonitor()
        );

        app.MapHub<Hubs.FlowHub>("/flow");

        app.UseResponseCompression();

        // this will run the asp.net app and wait until it is killed
        Console.WriteLine("Running FileFlows Server");

        app.Run($"http://0.0.0.0:{Port}/");                
        
        Console.WriteLine("Finished running FileFlows Server");

        WorkerManager.StopWorkers();
    }
}
