// SQLTool/Program.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SQLTool.Components;
using SQLTool.Services;

var builder = WebApplication.CreateBuilder(args);

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", p => p.RequireRole("SQLTool.User", "SQLTool.Admin"));
    options.AddPolicy("AdminPolicy", p => p.RequireRole("SQLTool.Admin"));
    options.FallbackPolicy = options.GetPolicy("UserPolicy");
});

// Services
var configDir = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["ConfigDir"] ?? "config");
builder.Services.AddSingleton<IConfigService>(sp => new ConfigService(configDir, builder.Configuration, sp.GetRequiredService<ILogger<ConfigService>>()));
builder.Services.AddScoped<IQueryEngine, QueryEngine>();
builder.Services.AddScoped<IExportService, ExportService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

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
app.MapRazorPages();
app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
