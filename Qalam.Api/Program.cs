using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Qalam.Core.MiddleWare;
using Serilog;
using Qalam.Core;
using Qalam.Infrastructure;
using Qalam.Infrastructure.context;
using Qalam.Service;
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
builder.Services.AddControllers();

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

#region Database Migration and Seeding

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDBContext>();

        Log.Information("Checking database connection...");

        // Check if database exists (without trying to create it)
        var canConnect = await context.Database.CanConnectAsync();

        if (!canConnect)
        {
            Log.Warning("Cannot connect to database. Please ensure the database exists on the server.");
            Log.Warning("Skipping migrations and seeding...");
        }
        else
        {
            Log.Information("Database connection successful. Applying migrations...");

            // Apply any pending migrations (won't try to create database if it exists)
            await context.Database.MigrateAsync();

            Log.Information("Database migrations applied successfully");

            // Check if seeding is needed (check if _SeedingHistory table exists and has our migration)
            var seedingApplied = await context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_SeedingHistory'")
                .FirstOrDefaultAsync();

            if (seedingApplied > 0)
            {
                // Check if this specific seeding has been completed
                var seedingCompleted = await context.Database
                    .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM [_SeedingHistory] WHERE [MigrationId] = '20260111200000_SeedInitialData' AND [SeedingCompleted] = 1")
                    .FirstOrDefaultAsync();

                if (seedingCompleted == 0)
                {
                    Log.Information("Starting database seeding...");

                    // Seed all data
                    await Qalam.Infrastructure.Seeding.DatabaseSeeder.SeedAllAsync(context);

                    // Mark seeding as completed
                    await context.Database.ExecuteSqlRawAsync(@"
                        UPDATE [_SeedingHistory] 
                        SET [SeedingCompleted] = 1, [SeedingCompletedAt] = GETUTCDATE()
                        WHERE [MigrationId] = '20260111200000_SeedInitialData'
                    ");

                    Log.Information("Database seeding completed successfully!");
                }
                else
                {
                    Log.Information("Database seeding already completed, skipping...");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying migrations or seeding the database");
        // Don't throw - allow the app to start even if seeding fails
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

