using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Core;
using Qalam.Core.MiddleWare;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeder;
using Qalam.Infrastructure.Seeding;
using Qalam.Service;
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
builder.Services.AddControllers();

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

		Log.Information("Checking database and applying migrations...");

		// Apply migrations - this will create the database if it doesn't exist
		// MigrateAsync() handles everything: creates DB, applies all pending migrations
		await context.Database.MigrateAsync();

		Log.Information("Database migrations applied successfully");

		// Now seed the data
		Log.Information("Starting database seeding...");

		// Seed all data using our seeders
		//   await Qalam.Infrastructure.Seeding.DatabaseSeeder.SeedAllAsync(context);

		// Seed Identity data (roles and admin user)
		Log.Information("Seeding roles and admin user...");
		var roleManager = services.GetRequiredService<RoleManager<Role>>();
		var userManager = services.GetRequiredService<UserManager<User>>();

		await RolesSeeder.SeedAsync(roleManager);
		await AdminUserSeeder.SeedAsync(userManager);

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
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "Qalam API v1");
	c.RoutePrefix = "swagger";
});

// Redirect root to Swagger in Development only
if (app.Environment.IsDevelopment())
{
	app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
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

