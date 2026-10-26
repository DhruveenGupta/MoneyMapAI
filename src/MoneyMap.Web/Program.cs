using Microsoft.AspNetCore.HttpOverrides;
using MoneyMap.Ai;
using MoneyMap.Application;
using MoneyMap.Application.Ai;
using MoneyMap.Persistence;
using MoneyMap.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Render (and similar hosts) assign the port at runtime via $PORT instead of a fixed one.
var hostingPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(hostingPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{hostingPort}");
}

// Local JSON file store, defaults to App_Data under the content root (gitignored).
// Override with Persistence:DataDirectory in config.
var dataDirectory = builder.Configuration["Persistence:DataDirectory"];
if (string.IsNullOrWhiteSpace(dataDirectory))
{
    dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
}

builder.Services.AddSingleton(new JsonFileBudgetProfileRepositoryOptions
{
    DataDirectory = dataDirectory,
    EnableEncryptionAtRest = builder.Configuration.GetValue("Persistence:EnableEncryptionAtRest", true),
});
builder.Services.AddSingleton<IBudgetProfileRepository, JsonFileBudgetProfileRepository>();

builder.Services.AddSingleton<IClock, SystemClock>();
// Scoped per circuit so each connected user gets their own in-memory profile cache.
builder.Services.AddScoped<BudgetProfileService>();

builder.Services.Configure<AnthropicAiAssistantOptions>(builder.Configuration.GetSection(AnthropicAiAssistantOptions.SectionName));
builder.Services.AddSingleton<RuleBasedAiAssistant>();
builder.Services.AddHttpClient<AnthropicAiAssistant>();
builder.Services.AddScoped<AiAssistantCoordinator>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AnthropicAiAssistantOptions>>().Value;
    var fallback = sp.GetRequiredService<RuleBasedAiAssistant>();
    var logger = sp.GetRequiredService<ILogger<AiAssistantCoordinator>>();
    IAiAssistant? remote = options.IsConfigured ? sp.GetRequiredService<AnthropicAiAssistant>() : null;
    return new AiAssistantCoordinator(fallback, logger, remote);
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Render (and most container hosts) terminate HTTPS at an edge proxy and forward plain HTTP to
// this container, so without this the app thinks every request is HTTP and redirect-loops trying
// to force HTTPS. KnownNetworks/KnownProxies are cleared because the proxy's IP isn't fixed.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaderOptions.KnownIPNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
