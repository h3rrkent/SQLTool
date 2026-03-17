// SQLTool/Program.cs
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SQLTool.Components;
using SQLTool.Services;

var builder = WebApplication.CreateBuilder(args);

// Auth
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", p => p.RequireRole("SQLTool.User", "SQLTool.Admin"));
    options.AddPolicy("AdminPolicy", p => p.RequireRole("SQLTool.Admin"));
    options.FallbackPolicy = options.GetPolicy("UserPolicy");
});

// Services
var configDir = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["ConfigDir"] ?? "config");
builder.Services.AddSingleton<IConfigService>(_ => new ConfigService(configDir, builder.Configuration));
builder.Services.AddScoped<IQueryEngine, QueryEngine>();
builder.Services.AddScoped<IExportService, ExportService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
