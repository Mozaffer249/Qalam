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

            // Messaging Services
            services.AddSingleton<IRabbitMQService, RabbitMQService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ISmsService, SmsService>();
            services.AddTransient<IPushNotificationService, PushNotificationService>();
            services.AddTransient<IMessageTrackingService, MessageTrackingService>();

            // Teacher Registration Services
            services.AddTransient<IOtpService, OtpService>();
            services.AddTransient<IAuthLoginOtpHelper, AuthLoginOtpHelper>();
            services.AddScoped<IAuthSettingsProvider, AuthSettingsProvider>();
            services.AddTransient<IFileStorageService, FileStorageService>();
            services.AddTransient<ITeacherRegistrationService, TeacherRegistrationService>();
            services.AddTransient<ITeacherManagementService, TeacherManagementService>();
            services.AddTransient<ITeacherRegistrationRequirementProvider, TeacherRegistrationRequirementProvider>();
            services.AddTransient<ITeacherRegistrationCompletionService, TeacherRegistrationCompletionService>();
            services.AddTransient<ITeacherRegistrationStatusService, TeacherRegistrationStatusService>();

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
            services.AddTransient<IEducationFilterService, EducationFilterService>();
            services.AddTransient<ITeacherCourseService, TeacherCourseService>();

            // Course / Schedule Services
            services.AddTransient<IScheduleGenerationService, ScheduleGenerationService>();
            services.AddTransient<ITeacherAvailabilityCalendarService, TeacherAvailabilityCalendarService>();
            services.AddTransient<IEnrollmentApprovalService, EnrollmentApprovalService>();

            // Open Session Request services (Scenario 2)
            services.AddTransient<ITeacherMatchingService, TeacherMatchingService>();
            services.AddTransient<IOpenSessionRequestTargetingService, OpenSessionRequestTargetingService>();
            services.AddTransient<ITargetedOpenSessionRequestValidator, TargetedOpenSessionRequestValidator>();
            services.AddTransient<IOfferConversationService, OfferConversationService>();

            return services;
        }
    }
}

