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
            services.AddSingleton<ILegalContentSanitizer, LegalContentSanitizer>();
            services.AddTransient<ILegalConsentService, LegalConsentService>();
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
            services.AddSingleton<IEmailDeliverabilityChecker, EmailDeliverabilityChecker>();
            services.AddScoped<IEmailSuppressionService, EmailSuppressionService>();
            services.AddScoped<IChatEmailNotifier, ChatEmailNotifier>();

            // Teacher Registration Services
            services.AddTransient<IOtpService, OtpService>();
            services.AddTransient<IAuthLoginOtpHelper, AuthLoginOtpHelper>();
            services.AddScoped<IAuthSettingsProvider, AuthSettingsProvider>();
            services.AddScoped<ITeacherAccessSettingsProvider, TeacherAccessSettingsProvider>();
            services.AddScoped<IOsrNotificationSettingsProvider, OsrNotificationSettingsProvider>();
            services.AddTransient<IFileStorageService, FileStorageService>();
            services.AddSingleton<IMediaUrlResolver, MediaUrlResolver>();
            services.AddTransient<ITeacherRegistrationService, TeacherRegistrationService>();
            services.AddTransient<ITeacherRegistrationSubmitService, TeacherRegistrationSubmitService>();
            services.AddTransient<ITeacherManagementService, TeacherManagementService>();
            services.AddTransient<ITeacherAccountDeletionService, TeacherAccountDeletionService>();
            services.AddTransient<ITeacherSubjectAdminService, TeacherSubjectAdminService>();
            services.AddTransient<INationalityProvider, NationalityProvider>();
            services.AddTransient<ITeacherRegistrationRequirementProvider, TeacherRegistrationRequirementProvider>();
            services.AddTransient<ITeacherRegistrationCompletionService, TeacherRegistrationCompletionService>();
            services.AddTransient<ITeacherRegistrationStatusService, TeacherRegistrationStatusService>();
            services.AddTransient<ITeacherLifecycleEmailService, TeacherLifecycleEmailService>();
            services.AddTransient<ITeacherDomainQuestionProvider, TeacherDomainQuestionProvider>();
            services.AddTransient<ITeacherDomainQuestionSubmitService, TeacherDomainQuestionSubmitService>();
            services.AddTransient<ITeacherDomainQuestionStatusService, TeacherDomainQuestionStatusService>();
            services.AddTransient<ITeacherDomainSubjectCascadeService, TeacherDomainSubjectCascadeService>();
            services.AddTransient<ITeacherDomainApprovalService, TeacherDomainApprovalService>();
            services.AddTransient<ITeacherReviewCorrectionService, TeacherReviewCorrectionService>();

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
            services.AddTransient<IEducationDeleteGuardService, EducationDeleteGuardService>();
            services.AddTransient<ITeacherCourseService, TeacherCourseService>();
            services.AddTransient<ITeacherEnrollmentService, TeacherEnrollmentService>();
            services.AddTransient<ITeacherSubjectRepertoireService, TeacherSubjectRepertoireService>();
            services.AddTransient<ITeacherContentService, TeacherContentService>();

            // Course / Schedule Services
            services.AddTransient<IScheduleGenerationService, ScheduleGenerationService>();
            services.AddTransient<ITeacherAvailabilityCalendarService, TeacherAvailabilityCalendarService>();
            services.AddTransient<IEnrollmentApprovalService, EnrollmentApprovalService>();
            services.AddTransient<IGuardianChildrenService, GuardianChildrenService>();
            services.AddTransient<IUserProfileService, UserProfileService>();
            services.AddTransient<IStudentEnrollmentQueryService, StudentEnrollmentQueryService>();
            services.AddTransient<ISessionLifecycleService, SessionLifecycleHelper>();
            services.AddTransient<ISessionPresenceService, SessionPresenceService>();
            services.AddTransient<ISessionReviewService, SessionReviewService>();
            services.AddTransient<IRefundService, RefundService>();
            services.AddTransient<IAdminFinanceService, AdminFinanceService>();
            services.AddTransient<IAdminFinanceTransactionService, AdminFinanceTransactionService>();
            services.AddTransient<ITeacherFinanceImpactService, TeacherFinanceImpactService>();
            services.AddTransient<IAdminEnrollmentQueryService, AdminEnrollmentQueryService>();
            services.AddTransient<IAdminSessionActionService, AdminSessionActionService>();
            services.AddTransient<ISessionAuditService, SessionAuditService>();
            services.AddTransient<ISessionComplaintService, SessionComplaintService>();
            services.AddTransient<IComplaintResolutionOrchestrator, ComplaintResolutionOrchestrator>();
            services.AddTransient<IEnrollmentCancellationService, EnrollmentCancellationService>();
            services.AddTransient<IEnrollmentCompletionService, EnrollmentCompletionService>();
            services.AddTransient<ITeacherEarningService, TeacherEarningService>();
            services.AddTransient<ITeacherFinanceDetailService, TeacherFinanceDetailService>();
            services.AddTransient<IPayoutService, PayoutService>();

            // Live session (RTC) — swap via LiveSession:Provider + new ILiveSessionProvider impl
            services.Configure<Qalam.Data.Helpers.LiveSessionSettings>(
                configuration.GetSection(Qalam.Data.Helpers.LiveSessionSettings.SectionName));
            services.AddTransient<LiveKitLiveSessionProvider>();
            services.AddTransient<ILiveSessionProvider>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Qalam.Data.Helpers.LiveSessionSettings>>().Value;
                var provider = settings.Provider ?? LiveKitLiveSessionProvider.Name;
                if (provider.Equals(LiveKitLiveSessionProvider.Name, StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<LiveKitLiveSessionProvider>();

                throw new InvalidOperationException(
                    $"Unsupported live session provider '{provider}'. Implement ILiveSessionProvider and register it.");
            });
            services.AddTransient<ILiveSessionAccessService, LiveSessionAccessService>();
            services.AddTransient<ILivePresenceWebhookService, LivePresenceWebhookService>();

            // Open Session Request services (Scenario 2)
            services.AddTransient<ITeacherMatchingService, TeacherMatchingService>();
            services.AddTransient<IOpenSessionRequestTargetingService, OpenSessionRequestTargetingService>();
            services.AddTransient<ITargetedOpenSessionRequestValidator, TargetedOpenSessionRequestValidator>();
            services.AddTransient<IOfferConversationService, OfferConversationService>();
            services.AddTransient<IOpenSessionOfferAcceptanceService, OpenSessionOfferAcceptanceService>();
            services.AddTransient<IOpenSessionRequestPublishService, OpenSessionRequestPublishService>();
            services.AddTransient<IStudentInvitationInboxService, StudentInvitationInboxService>();
            services.AddTransient<IOpenSessionRequestReleaseService, OpenSessionRequestReleaseService>();
            services.AddTransient<ISessionAvailabilityMatchService, SessionAvailabilityMatchService>();

            // Pricing
            services.AddTransient<IPricingEngine, PricingEngine>();
            services.AddTransient<IStudentCoursePriceResolver, StudentCoursePriceResolver>();
            services.AddTransient<IFreeSessionPolicyService, FreeSessionPolicyService>();
            services.AddTransient<IFreeSessionLedgerReadService, FreeSessionLedgerReadService>();
            services.AddTransient<IPricingMarketResolver, PricingMarketResolver>();
            services.AddTransient<IPricingSnapshotWriter, PricingSnapshotWriter>();
            services.AddTransient<ITargetedOpenSessionRequestPricingService, TargetedOpenSessionRequestPricingService>();
            services.AddTransient<IDomainRatePropagationService, DomainRatePropagationService>();
            services.AddTransient<IPricingAdminService, PricingAdminService>();
            services.AddTransient<IPricingMarketService, PricingMarketService>();
            services.AddTransient<ITeacherLevelProgressionService, TeacherLevelProgressionService>();

            return services;
        }
    }
}

