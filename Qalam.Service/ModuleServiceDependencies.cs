using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service
{
    public static class ModuleServiceDependencies
    {
        public static IServiceCollection AddServiceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // Authentication & Security services
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<ITwoFactorAuthenticationService, TwoFactorAuthenticationService>();
            services.AddSingleton<IRateLimitingService, RateLimitingService>();
            services.AddTransient<IAuditService, AuditService>();
            services.AddTransient<ISessionManagementService, SessionManagementService>();
            services.AddTransient<IPasswordSecurityService, PasswordSecurityService>();
            services.AddTransient<ISecurityNotificationService, SecurityNotificationService>();
            services.AddTransient<IRiskAssessmentService, RiskAssessmentService>();

            // Register EmailService
            services.AddHttpClient<IEmailService, EmailService>();

            // Add memory cache for rate limiting and IP blocking
            services.AddMemoryCache();

            return services;
        }
    }
}

