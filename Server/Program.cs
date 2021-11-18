var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseDefaultFiles();

var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".br"] = "text/plain";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
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

if (FileFlows.Server.Globals.IsDevelopment == false)
    FileFlows.Server.Helpers.DbHelper.StartMySqlServer();
FileFlows.Server.Helpers.DbHelper.CreateDatabase();
FileFlows.Server.Helpers.DbHelper.UpgradeDatabase();

System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
FileFlows.Server.Globals.Version = fvi.FileVersion;



FileFlows.Server.Helpers.PluginHelper.ScanForPlugins();

FileFlows.Server.Workers.LibraryWorker.ResetProcessing();

FileFlows.Server.Workers.Worker.StartWorkers();

// this will run the asp.net app and wait until it is killed
app.Run();

FileFlows.Server.Workers.Worker.StopWorkers();
