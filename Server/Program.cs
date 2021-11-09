var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<FileFlow.Server.ExceptionMiddleware>();
app.UseRouting();

if (app.Environment.IsDevelopment())
    app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
//app.UseCors(builder => builder.WithOrigins("http://localhost:5000").AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

FileFlow.Shared.Logger.Instance = FileFlow.Server.Logger.Instance;

FileFlow.Server.Helpers.PluginHelper.ScanForPlugins();

FileFlow.Server.Workers.LibraryWorker.ResetProcessing();

FileFlow.Server.Workers.Worker.StartWorkers();
app.Run();
FileFlow.Server.Workers.Worker.StopWorkers();
