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

app.UseMiddleware<FileFlow.Server.ExceptionMiddleware>();
app.UseRouting();

FileFlow.Server.Globals.IsDevelopment = app.Environment.IsDevelopment();

if (FileFlow.Server.Globals.IsDevelopment)
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

FileFlow.Shared.Logger.Instance = FileFlow.Server.Logger.Instance;

if (FileFlow.Server.Globals.IsDevelopment == false)
    FileFlow.Server.Helpers.DbHelper.StartMySqlServer();
FileFlow.Server.Helpers.DbHelper.CreateDatabase();

System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
FileFlow.Server.Globals.Version = fvi.FileVersion;



FileFlow.Server.Helpers.PluginHelper.ScanForPlugins();

FileFlow.Server.Workers.LibraryWorker.ResetProcessing();

FileFlow.Server.Workers.Worker.StartWorkers();

// this will run the asp.net app and wait until it is killed
app.Run();

FileFlow.Server.Workers.Worker.StopWorkers();
