var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

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

app.UseMiddleware<FileFlows.Server.ExceptionMiddleware>();
app.UseRouting();

FileFlows.Server.Globals.IsDevelopment = app.Environment.IsDevelopment();

if (FileFlows.Server.Globals.IsDevelopment)
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

FileFlows.Shared.Logger.Instance = FileFlows.Server.Logger.Instance;

//if (FileFlows.Server.Globals.IsDevelopment == false)
//    FileFlows.Server.Helpers.DbHelper.StartMySqlServer();
FileFlows.Server.Helpers.DbHelper.CreateDatabase();

System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
FileFlows.Server.Globals.Version = fvi?.FileVersion ?? String.Empty;

Console.WriteLine(new string('=', 50));
Console.WriteLine("Starting File Flows " + fvi.FileVersion);
Console.WriteLine(new string('=', 50));

FileFlows.Shared.Helpers.HttpHelper.Client = new HttpClient();

using var pl = new FileFlows.Server.Helpers.PluginHelper();
pl.ScanForPlugins();

if (FileFlows.Server.Globals.Demo == false)
{
    FileFlows.Server.Workers.LibraryWorker.ResetProcessing();
    FileFlows.Server.Workers.Worker.StartWorkers();
}

// this will run the asp.net app and wait until it is killed
app.Run();


if (FileFlows.Server.Globals.Demo == false)
{
    FileFlows.Server.Workers.Worker.StopWorkers();
}
