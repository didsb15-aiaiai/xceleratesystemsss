using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using XcelerateLinks.Mvc.Services;             // TokenHandler, ApiClient, IApiClient
using XcelerateLinks.Models.ViewModels;
using XcelerateLinks.Mvc.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor so TokenHandler can access cookies
builder.Services.AddHttpContextAccessor();

// Register TokenHandler (delegating handler that attaches JWT from cookie)
builder.Services.AddTransient<TokenHandler>();

// Read API base URL from environment (Api__BaseUrl) or configuration and validate early
var apiBase = Environment.GetEnvironmentVariable("Api__BaseUrl")
              ?? builder.Configuration["Api:BaseUrl"]
              ?? throw new InvalidOperationException("Api:BaseUrl not configured in appsettings.json or environment variable Api__BaseUrl.");

builder.Logging.AddConsole();
Console.WriteLine($"[MVC] Api BaseUrl = {apiBase}");

// Ensure base ends with slash for relative URIs usage
if (!apiBase.EndsWith('/')) apiBase += '/';

// Helper to produce a dev-friendly primary handler (accept untrusted dev certs only in Development)
HttpMessageHandler CreatePrimaryHandler()
{
    if (builder.Environment.IsDevelopment())
    {
        return new HttpClientHandler
        {
            // Development convenience: accept self-signed / dev certs.
            // WARNING: DO NOT use in production.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }

    return new HttpClientHandler();
}

builder.Services.AddTransient<LoggingHandler>();

// Register named HttpClient "Api"
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

// Register typed API client that uses the same base and pipeline and attaches token via TokenHandler
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<TokenHandler>()
.ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

// Cookie authentication for MVC site
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";

        // Cookie settings - require HTTPS in production, allow SameAsRequest in Development
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication middleware
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();