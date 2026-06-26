using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Email;

namespace Qalam.Service.Implementations;

public class TeacherLifecycleEmailService : ITeacherLifecycleEmailService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IEmailService _emailService;
    private readonly PlatformSettings _platformSettings;
    private readonly ILogger<TeacherLifecycleEmailService> _logger;

    public TeacherLifecycleEmailService(
        ITeacherRepository teacherRepository,
        IEmailService emailService,
        IOptions<PlatformSettings> platformSettings,
        ILogger<TeacherLifecycleEmailService> logger)
    {
        _teacherRepository = teacherRepository;
        _emailService = emailService;
        _platformSettings = platformSettings.Value;
        _logger = logger;
    }

    public Task SendRegistrationReceivedAsync(int teacherId, CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildRegistrationReceivedSubject(),
            loginUrl => TeacherLifecycleEmailTemplates.BuildRegistrationReceivedHtml(loginUrl),
            "registration received",
            cancellationToken);

    public Task SendDocumentRejectedAsync(
        int teacherId,
        string documentLabel,
        string reason,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildDocumentRejectedSubject(documentLabel),
            loginUrl => TeacherLifecycleEmailTemplates.BuildDocumentRejectedHtml(loginUrl, documentLabel, reason),
            "document rejected",
            cancellationToken);

    public Task SendSubjectRejectedAsync(
        int teacherId,
        string subjectName,
        string reason,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildSubjectRejectedSubject(subjectName),
            loginUrl => TeacherLifecycleEmailTemplates.BuildSubjectRejectedHtml(loginUrl, subjectName, reason),
            "subject rejected",
            cancellationToken);

    public Task SendDomainVerificationRejectedAsync(
        int teacherId,
        string domainName,
        string reason,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildDocumentRejectedSubject(domainName),
            loginUrl => TeacherLifecycleEmailTemplates.BuildDocumentRejectedHtml(loginUrl, domainName, reason),
            "domain verification rejected",
            cancellationToken);

    public Task SendAccountActivatedAsync(int teacherId, CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildAccountActivatedSubject(),
            loginUrl => TeacherLifecycleEmailTemplates.BuildAccountActivatedHtml(loginUrl),
            "account activated",
            cancellationToken);

    public Task SendAccountBlockedAsync(int teacherId, string? reason, CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildAccountBlockedSubject(),
            _ => TeacherLifecycleEmailTemplates.BuildAccountBlockedHtml(reason),
            "account blocked",
            cancellationToken);

    public Task SendAccountUnblockedAsync(int teacherId, CancellationToken cancellationToken = default) =>
        TrySendAsync(
            teacherId,
            TeacherLifecycleEmailTemplates.BuildAccountUnblockedSubject(),
            loginUrl => TeacherLifecycleEmailTemplates.BuildAccountUnblockedHtml(loginUrl),
            "account unblocked",
            cancellationToken);

    private async Task TrySendAsync(
        int teacherId,
        string subject,
        Func<string, string> buildBody,
        string eventName,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = await ResolveTeacherEmailAsync(teacherId, cancellationToken);
            if (email == null)
                return;

            var loginUrl = ResolveLoginUrl();
            var body = buildBody(loginUrl);

            await _emailService.SendEmailAsync(email, subject, body, SendingStrategy.Queued);

            _logger.LogInformation(
                "Queued teacher lifecycle email ({Event}) for teacher {TeacherId}",
                eventName,
                teacherId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send teacher lifecycle email ({Event}) for teacher {TeacherId}",
                eventName,
                teacherId);
        }
    }

    private async Task<string?> ResolveTeacherEmailAsync(int teacherId, CancellationToken cancellationToken)
    {
        var emails = await _teacherRepository.GetEmailsByTeacherIdsAsync([teacherId], cancellationToken);
        var email = emails.FirstOrDefault(e => e.TeacherId == teacherId).Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning(
                "Skipping teacher lifecycle email — no email on file for teacher {TeacherId}",
                teacherId);
            return null;
        }

        return email;
    }

    private string ResolveLoginUrl()
    {
        var configured = _platformSettings.WebAppBaseUrl?.Trim();
        if (string.IsNullOrEmpty(configured))
        {
            _logger.LogWarning(
                "PlatformSettings.WebAppBaseUrl is empty; using default {DefaultUrl}",
                TeacherLifecycleEmailTemplates.DefaultLoginUrl);
            return TeacherLifecycleEmailTemplates.DefaultLoginUrl;
        }

        return configured.TrimEnd('/') + "/";
    }
}
