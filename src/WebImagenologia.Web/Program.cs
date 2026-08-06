using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var sessionTimeoutMinutes = builder.Configuration.GetValue("Session:TimeoutMinutes", 30);
const long maxUploadBytes = 26 * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.Services.Configure<EsculapioApiOptions>(
    builder.Configuration.GetSection(EsculapioApiOptions.SectionName));
builder.Services.Configure<N8nOptions>(
    builder.Configuration.GetSection(N8nOptions.SectionName));

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHttpClient<IEsculapioApiClient, EsculapioApiClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EsculapioApiOptions>>().Value;
        var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });

    builder.Services.AddHttpClient(N8nWebhookClient.HttpClientName, (serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<N8nOptions>>().Value;
        var webhookUrl = options.WebhookUrl.EndsWith('/') ? options.WebhookUrl : $"{options.WebhookUrl}/";
        client.BaseAddress = new Uri(webhookUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    });
}

builder.Services.AddScoped<IN8nWebhookClient, N8nWebhookClient>();

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministradorOnly", policy => policy.RequireRole(RoleNames.Administrador));
    options.AddPolicy("RadiologoOnly", policy => policy.RequireRole(RoleNames.Radiologo));
    options.AddPolicy("OperadorOrAdmin", policy =>
        policy.RequireRole(RoleNames.Operador, RoleNames.Administrador));
    options.AddPolicy("RadiologoOrAdmin", policy =>
        policy.RequireRole(RoleNames.Radiologo, RoleNames.Administrador));
});

builder.Services.AddControllersWithViews();

ConfigureTestServices?.Invoke(builder.Services);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
    internal static Action<IServiceCollection>? ConfigureTestServices { get; set; }
}
