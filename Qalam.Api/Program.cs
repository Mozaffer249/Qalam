using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Qalam.Core.MiddleWare;
using Serilog;
using Qalam.Core;
using Qalam.Infrastructure;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeder;
using Qalam.Service;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configure host options to prevent crash on background service failure
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Add services to the container.
builder.Services.AddControllers();

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connection to SQL Server
builder.Services.AddDbContext<ApplicationDBContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("dbcontext"));
});

#region Dependency Injections

// Configure Settings
builder.Services.Configure<Qalam.Service.Models.SecuritySettings>(
    builder.Configuration.GetSection("SecuritySettings"));

// Service Registration
builder.Services.AddInfrastructureDependencies()
                .AddServiceDependencies(builder.Configuration)
                .AddCoreDependencies()
                .AddServiceRegisteration(builder.Configuration);

#endregion

#region Localization

// Localization
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization(opt => { opt.ResourcesPath = ""; });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    List<CultureInfo> supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("en-US"),
        new CultureInfo("ar-EG")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

#endregion

#region CORS

var CORS = "_cors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CORS,
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "*" };

            if (allowedOrigins.Length == 1 && allowedOrigins[0] == "*")
            {
                // Development: Allow any origin
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production: Restrict to specific origins
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
});

#endregion

#region Serilog

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Services.AddSerilog();

#endregion

var app = builder.Build();

#region Database Initialization & Seeding

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var databaseSeeder = services.GetRequiredService<DatabaseSeeder>();
        await databaseSeeder.InitializeAsync();

        var roleSeeder = services.GetRequiredService<RoleSeeder>();
        await roleSeeder.SeedAsync();

        var userSeeder = services.GetRequiredService<UserSeeder>();
        await userSeeder.SeedAsync();

        Log.Information("Database initialization and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database.");
    }
}

#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect root to Swagger in Development
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

#region Localization Middleware
var options = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(options!.Value);
#endregion

// Security headers middleware (first)
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseMiddleware<ErrorHandlerMiddleware>(); // Error Handling Middleware

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(CORS);

// Rate limiting middleware (before authentication)
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Audit logging middleware (after authentication)
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers();

app.Run();

