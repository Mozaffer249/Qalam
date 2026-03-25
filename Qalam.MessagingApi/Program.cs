using Microsoft.EntityFrameworkCore;
using Qalam.MessagingApi.BackgroundServices;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Services;
using Qalam.MessagingApi.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Services.AddSerilog();

// Prevent crash on background service failure
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Database — own MessagingDbContext (NOT shared IdentityDbContext)
builder.Services.AddDbContext<MessagingDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MessagingDb"));
});

// Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("SmsSettings"));
builder.Services.Configure<PushSettings>(builder.Configuration.GetSection("PushNotificationSettings"));
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.Configure<WasabiSettings>(builder.Configuration.GetSection("WasabiSettings"));

// Services
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IMessageTrackingService, MessageTrackingService>();
builder.Services.AddScoped<IWasabiStorageService, WasabiStorageService>();

// Background consumers
builder.Services.AddHostedService<EmailConsumerService>();
builder.Services.AddHostedService<SmsConsumerService>();
builder.Services.AddHostedService<PushConsumerService>();
builder.Services.AddHostedService<TeacherDocUploadConsumer>();
builder.Services.AddHostedService<ProfilePicUploadConsumer>();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Qalam Messaging API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        await context.Database.MigrateAsync();
        Log.Information("MessagingDb migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to apply MessagingDb migrations");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Qalam Messaging API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.MapControllers();

Log.Information("Qalam.MessagingApi starting...");
app.Run();
