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

            // Teacher Registration Services
            services.AddTransient<IOtpService, OtpService>();
            services.AddTransient<IFileStorageService, FileStorageService>();
            services.AddTransient<ITeacherRegistrationService, TeacherRegistrationService>();
            services.AddTransient<ITeacherManagementService, TeacherManagementService>();

            // Enum Services
            services.AddTransient<IEnumService, EnumService>();

            // Add memory cache for rate limiting and IP blocking
            services.AddMemoryCache();

            // Education Management Services
            services.AddTransient<IEducationDomainService, EducationDomainService>();
            services.AddTransient<ICurriculumService, CurriculumService>();
            services.AddTransient<IGradeService, GradeService>();
            services.AddTransient<ISubjectService, SubjectService>();
            services.AddTransient<IContentManagementService, ContentManagementService>();
            services.AddTransient<IQuranService, QuranService>();
            services.AddTransient<ITeachingConfigurationService, TeachingConfigurationService>();

            return services;
        }
    }
}

