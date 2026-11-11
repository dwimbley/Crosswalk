using Crosswalk.App.Components;
using Crosswalk.App.Endpoints;
using Crosswalk.App.Security;
using Crosswalk.App.Services;
using Crosswalk.Core.Cache;
using Crosswalk.Core.Configuration;
using Crosswalk.Core.Demo;
using Crosswalk.Core.Jira;
using Crosswalk.Core.Secrets;
using Crosswalk.Core.Sync;

var builder = WebApplication.CreateBuilder(args);

var isDemo = args.Contains("--demo");
var isIIS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_POOL_ID"))
         || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH"));
var port = 5391;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--port" && int.TryParse(args[i + 1], out var p))
        port = p;
}

// When running standalone, bind to localhost only. Under IIS, let IIS manage bindings.
if (!isIIS)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
}

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Config directory: use CROSSWALK_DATA env var if set (useful for IIS), else default
var configDir = Environment.GetEnvironmentVariable("CROSSWALK_DATA");
var configStore = configDir != null ? new FileConfigStore(configDir) : new FileConfigStore();

// Ensure the directory exists early so we get a clear error
try
{
    Directory.CreateDirectory(configStore.ConfigDirectory);
    Directory.CreateDirectory(Path.Combine(configStore.ConfigDirectory, "cache"));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: Cannot create config directory '{configStore.ConfigDirectory}': {ex.Message}");
    Console.Error.WriteLine("Set CROSSWALK_DATA environment variable to a writable path.");
    return;
}

builder.Services.AddSingleton<IConfigStore>(configStore);
builder.Services.AddSingleton<ICacheStore>(new FileCacheStore(configStore.ConfigDirectory));

// Secret store
if (DevSecretStore.IsEnabled || isIIS)
{
    // Under IIS, DPAPI may not work with the app pool identity — use dev store by default
    // Set CROSSWALK_DEV_SECRETS=false explicitly to force DPAPI under IIS
    var forceDpapi = string.Equals(
        Environment.GetEnvironmentVariable("CROSSWALK_DEV_SECRETS"), "false", StringComparison.OrdinalIgnoreCase);

    if (forceDpapi && OperatingSystem.IsWindows())
    {
        builder.Services.AddSingleton<ISecretStore>(new WindowsSecretStore(configStore.ConfigDirectory));
    }
    else
    {
        builder.Services.AddSingleton<ISecretStore>(sp =>
            new DevSecretStore(configStore.ConfigDirectory, sp.GetRequiredService<ILogger<DevSecretStore>>()));
    }
}
else if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<ISecretStore>(new WindowsSecretStore());
}
else
{
    builder.Services.AddSingleton<ISecretStore>(sp =>
        new DevSecretStore(configStore.ConfigDirectory, sp.GetRequiredService<ILogger<DevSecretStore>>()));
}

// Jira HTTP client
builder.Services.AddHttpClient("Jira", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IJiraClient, JiraClient>();

// Sync / Demo
if (isDemo)
{
    var demoSync = new DemoSyncService();
    builder.Services.AddSingleton(demoSync);
    builder.Services.AddSingleton(new AppState(demoSync));
}
else
{
    builder.Services.AddSingleton<SyncService>();
    builder.Services.AddSingleton(sp => new AppState(sp.GetRequiredService<SyncService>()));
    builder.Services.AddHostedService<BackgroundRefreshService>();
}

// Browser launcher — only when running standalone, not under IIS
if (!isIIS && (!builder.Environment.IsDevelopment() || args.Contains("--open")))
{
    builder.Services.AddHostedService<BrowserLauncher>();
}

var app = builder.Build();

// Detailed errors in development or when CROSSWALK_DETAILED_ERRORS is set
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("CROSSWALK_DETAILED_ERRORS") == "true")
{
    app.UseDeveloperExceptionPage();
}

// Security — skip localhost-only check when behind IIS (IIS handles binding restrictions)
if (!isIIS)
{
    app.UseMiddleware<LocalOnlyMiddleware>();
}
app.UseAntiforgery();

// Static assets
app.MapStaticAssets();

// API endpoints
app.MapTicketEndpoints();
app.MapConnectionEndpoints();
app.MapRefreshEndpoints();
app.MapSettingsEndpoints();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Console.WriteLine($"Crosswalk is running{(isIIS ? " (IIS)" : $" at http://127.0.0.1:{port}")}");
Console.WriteLine($"  Config: {configStore.ConfigFilePath}");
if (isDemo) Console.WriteLine("  Mode: DEMO (using fixture data, no Jira connections)");

app.Run();
