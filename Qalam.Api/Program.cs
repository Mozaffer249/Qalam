using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Api.Configurations;
using Qalam.Core;
using Qalam.Core.Bases;
using Qalam.Core.MiddleWare;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeder;
using Qalam.Infrastructure.Seeding;
using Qalam.Service;
using Scalar.AspNetCore;
using Serilog;
using System.Globalization;

// Force Gregorian calendar for all cultures to prevent Hijri dates in database
var defaultCulture = new CultureInfo("en-US");
defaultCulture.DateTimeFormat.Calendar = new GregorianCalendar();
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var builder = WebApplication.CreateBuilder(args);

// Configure host options to prevent crash on background service failure
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Add services to the container.
// Configure DataAnnotations localization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResources));
    });

// Configure API behavior to use custom validation response format with localized errors
builder.Services.ConfigureValidationBehavior();

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Connection to SQL Server
builder.Services.AddDbContext<ApplicationDBContext>(option =>
{
    var connectionString = builder.Configuration.GetConnectionString("dbcontext");
    option.UseSqlServer(connectionString, sqlOptions =>
    {
        // Ensure Gregorian calendar is used for date operations
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

#region Dependency Injections

// Configure Settings
builder.Services.Configure<Qalam.Service.Models.SecuritySettings>(
    builder.Configuration.GetSection("SecuritySettings"));
builder.Services.Configure<Qalam.Data.Helpers.EnrollmentSettings>(
    builder.Configuration.GetSection("EnrollmentSettings"));
builder.Services.Configure<Qalam.Data.Helpers.PaymentSettings>(
    builder.Configuration.GetSection("PaymentSettings"));
builder.Services.Configure<Qalam.Data.Helpers.OpenSessionOfferSettings>(
    builder.Configuration.GetSection("OpenSessionOfferSettings"));

// Background Services
builder.Services.AddHostedService<Qalam.Service.BackgroundServices.EnrollmentExpirationService>();
builder.Services.AddHostedService<Qalam.Service.BackgroundServices.SessionOfferExpirationService>();

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
    // Create cultures with Gregorian calendar
    var enCulture = new CultureInfo("en-US");

    var arCulture = new CultureInfo("ar-EG");
    // Force Arabic culture to use Gregorian calendar instead of Hijri
    arCulture.DateTimeFormat.Calendar = new GregorianCalendar();

    List<CultureInfo> supportedCultures = new List<CultureInfo>
    {
        enCulture,
        arCulture
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
            // Cors:AllowedOrigins can arrive via two formats:
            //   1. appsettings.json   — JSON array, binds natively to string[]
            //   2. env Cors__AllowedOrigins=a,b,c — single CSV string; .NET does NOT auto-split it,
            //      so GetSection().Get<string[]>() returns one element whose value is the whole CSV,
            //      and WithOrigins(...) then registers that literal CSV as a single (never-matching) origin.
            // Detect the scalar/CSV case explicitly and split.
            string[] allowedOrigins;
            var scalarCsv = builder.Configuration["Cors:AllowedOrigins"];
            if (!string.IsNullOrWhiteSpace(scalarCsv) && scalarCsv.Contains(','))
            {
                allowedOrigins = scalarCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else
            {
                allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "*" };
            }

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

#region Database Migration and Seeding

// Apply migrations and seed database.
// Auto-migration policy:
//   - Development / Staging: ON by default.
//   - Production: OFF unless MIGRATE_ON_STARTUP=true (see docs/OPERATIONS_RUNBOOK.md for the manual prod migration step).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDBContext>();
        var migrateOnStartupRaw = builder.Configuration["MIGRATE_ON_STARTUP"];
        var migrateOnStartupOverride = string.Equals(migrateOnStartupRaw, "true", StringComparison.OrdinalIgnoreCase);
        // var shouldMigrate = !app.Environment.IsProduction() || migrateOnStartupOverride;

        // if (shouldMigrate)
        // {
        Log.Information("Checking database and applying migrations (environment: {Env}, override: {Override})...",
            app.Environment.EnvironmentName, migrateOnStartupOverride);
        await context.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
        // }
        // else
        // {
        //     Log.Information(
        //         "Skipping auto-migration in {Env} (MIGRATE_ON_STARTUP={Value}). " +
        //         "Run `dotnet ef database update` manually before promoting a build (see OPERATIONS_RUNBOOK.md).",
        //         app.Environment.EnvironmentName,
        //         migrateOnStartupRaw ?? "(unset)");
        // }

        // Seed reference data (idempotent — safe to run every boot).
        Log.Information("Starting database seeding...");
        await Qalam.Infrastructure.Seeding.DatabaseSeeder.SeedAllAsync(context);

        // Seed Identity data (roles always; default admin only when SEED_DEFAULT_ADMIN=true).
        Log.Information("Seeding roles and (gated) admin user...");
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var seederLogger = services.GetService<ILogger<Program>>();

        await RolesSeeder.SeedAsync(roleManager);
        await AdminUserSeeder.SeedAsync(userManager, builder.Configuration, seederLogger);

        Log.Information("Database seeding completed successfully!");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying migrations or seeding the database");
        // Don't throw - allow the app to start even if seeding fails
    }
}
#endregion

// Configure the HTTP request pipeline.
// Enable Swagger/OpenAPI in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Qalam API v1");
    c.RoutePrefix = "swagger";
});

// Add Scalar UI as a modern alternative to Swagger UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Qalam API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithEndpointPrefix("/scalar/{documentName}")
        .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
});

// Redirect root to Scalar UI in Development
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

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

