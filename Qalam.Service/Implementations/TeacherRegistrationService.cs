using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationService : ITeacherRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<TeacherRegistrationService> _logger;
    private readonly ITeacherSubjectRepository _subjectRepository;
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly IAuthLoginOtpHelper _authLoginOtpHelper;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherReviewCorrectionService _reviewCorrectionService;
    private readonly ITeacherDomainQuestionStatusService _domainQuestionStatusService;

    public TeacherRegistrationService(
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        IAuthenticationService authService,
        ILogger<TeacherRegistrationService> logger,
        ITeacherSubjectRepository subjectRepository,
        ITeacherAvailabilityRepository availabilityRepository,
        IAuthLoginOtpHelper authLoginOtpHelper,
        ITeacherLifecycleEmailService lifecycleEmailService,
        ITeacherRegistrationCompletionService completionService,
        ITeacherReviewCorrectionService reviewCorrectionService,
        ITeacherDomainQuestionStatusService domainQuestionStatusService)
    {
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _authService = authService;
        _logger = logger;
        _subjectRepository = subjectRepository;
        _availabilityRepository = availabilityRepository;
        _authLoginOtpHelper = authLoginOtpHelper;
        _lifecycleEmailService = lifecycleEmailService;
        _completionService = completionService;
        _reviewCorrectionService = reviewCorrectionService;
        _domainQuestionStatusService = domainQuestionStatusService;
    }

    public async Task<PhoneVerificationDto> CreateBasicAccountAsync(string fullPhoneNumber, string? email = null)
    {
        var accountEmail = _authLoginOtpHelper.ResolveAccountEmail(email, fullPhoneNumber);

        var user = new User
        {
            UserName = fullPhoneNumber,
            PhoneNumber = fullPhoneNumber,
            PhoneNumberConfirmed = true,
            Email = accountEmail,
            NormalizedEmail = accountEmail.ToUpperInvariant(),
            IsActive = false,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user account: {Errors}", errors);
            throw new Exception($"Failed to create account: {errors}");
        }

        // Assign Teacher role
        await _userManager.AddToRoleAsync(user, Roles.Teacher);

        // Generate JWT token
        var jwtResult = await _authService.GetJWTToken(user);

        _logger.LogInformation("New teacher account created for user {UserId}", user.Id);

        return new PhoneVerificationDto
        {
            UserId = user.Id,
            Token = jwtResult.AccessToken,
            PhoneNumber = fullPhoneNumber
        };
    }

    public async Task<TeacherAccountDto> CompleteAccountAsync(
        int userId,
        string firstName,
        string lastName,
        string? email,
        string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found");
        }

        // Email uniqueness check — only when the email actually changes for this user.
        if (!string.IsNullOrWhiteSpace(email))
        {
            var trimmedEmail = email.Trim();
            var sameAsCurrent = !string.IsNullOrEmpty(user.Email)
                && string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase);
            if (!sameAsCurrent)
            {
                User? emailOwner;
                try
                {
                    emailOwner = await _userManager.FindByEmailAsync(trimmedEmail);
                }
                catch (InvalidOperationException)
                {
                    // Legacy duplicates already in the DB — treat as a hard collision.
                    throw new Exception("Email is already registered.");
                }
                if (emailOwner != null && emailOwner.Id != user.Id)
                {
                    throw new Exception("Email is already registered.");
                }
            }
            user.Email = trimmedEmail;
            user.NormalizedEmail = trimmedEmail.ToUpperInvariant();
        }
        else if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new Exception(
                "Email is required. Include email in this request or restart registration with email OTP.");
        }
        // When email is omitted, keep the address set during OTP (step 1–2).

        // Update user details
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IsActive = true;

        // Set password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var passwordResult = await _userManager.ResetPasswordAsync(user, token, password);

        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to set password: {errors}");
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            // Most common failure now is the Identity "DuplicateEmail" validator (RequireUniqueEmail).
            // Surface the message so the caller (handler) translates it to a 400.
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to update account: {errors}");
        }

        // Create Teacher profile
        var teacher = new Teacher
        {
            UserId = user.Id,
            Status = TeacherStatus.AwaitingDocuments,
            IsActive = true
        };

        await _teacherRepository.AddAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        // Generate new JWT token with updated user info
        var jwtResult = await _authService.GetJWTToken(user);

        _logger.LogInformation(
            "Teacher account completed for user {UserId}, teacher {TeacherId}",
            user.Id,
            teacher.Id);

        return new TeacherAccountDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber!,
            Token = jwtResult.AccessToken
        };
    }

    public async Task CompleteDocumentUploadAsync(
        int teacherId,
        bool isInSaudiArabia)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        var previousStatus = teacher?.Status ?? TeacherStatus.AwaitingDocuments;

        // Update teacher status to PendingVerification
        await _teacherRepository.UpdateStatusAsync(
            teacherId,
            TeacherStatus.PendingVerification);

        // Update teacher location
        var location = isInSaudiArabia
            ? TeacherLocation.InsideSaudiArabia
            : TeacherLocation.OutsideSaudiArabia;

        await _teacherRepository.UpdateLocationAsync(teacherId, location);

        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Document upload completed for teacher {TeacherId}, status set to PendingVerification",
            teacherId);

        if (previousStatus == TeacherStatus.AwaitingDocuments)
            await _lifecycleEmailService.SendRegistrationReceivedAsync(teacherId);
    }

    public async Task<RegistrationStepDto> GetNextRegistrationStepAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found");
        }

        // Check if Teacher profile exists
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);

        if (teacher == null)
        {
            // Step 2 complete, need Step 3
            return new RegistrationStepDto
            {
                CurrentStep = 2,
                NextStep = 3,
                NextStepName = "Complete Personal Information",
                IsRegistrationComplete = false
            };
        }

        // Teacher profile exists, check status
        switch (teacher.Status)
        {
            case TeacherStatus.AwaitingDocuments:
                return new RegistrationStepDto
                {
                    CurrentStep = 3,
                    NextStep = 4,
                    NextStepName = "Upload Documents",
                    IsRegistrationComplete = false
                };

            case TeacherStatus.PendingVerification:
                return await BuildPostRegistrationStepAsync(teacher.Id);

            case TeacherStatus.DocumentsRejected:
                var rejectedDocs = await _documentRepository.GetRejectedDocumentsAsync(teacher.Id);
                if (rejectedDocs.Count > 0)
                {
                    return new RegistrationStepDto
                    {
                        CurrentStep = 4,
                        NextStep = 4,
                        NextStepName = "Re-upload Rejected Documents",
                        IsRegistrationComplete = false,
                        Message = "Some of your documents were rejected. Please check the rejection reasons and re-upload.",
                        RejectedDocuments = rejectedDocs,
                        PendingCorrections = await _reviewCorrectionService.GetPendingCorrectionsAsync(teacher.Id)
                    };
                }

                return await BuildPostRegistrationStepAsync(teacher.Id);

            case TeacherStatus.Active:
            {
                var requiresAvailability = !await _availabilityRepository.HasAnyAvailabilityAsync(teacher.Id);
                return new RegistrationStepDto
                {
                    CurrentStep = 6,
                    NextStep = 0,
                    NextStepName = "Dashboard",
                    IsRegistrationComplete = true,
                    RequiresAvailabilitySetup = requiresAvailability,
                    Message = requiresAvailability
                        ? "Your account is active. Set your availability from the dashboard when you are ready."
                        : "Welcome back! Your teacher dashboard is ready."
                };
            }

            case TeacherStatus.Blocked:
                throw new Exception("Your account has been blocked. Please contact support.");

            default:
                throw new Exception("Unknown registration status");
        }
    }

    private async Task<RegistrationStepDto> BuildPostRegistrationStepAsync(int teacherId)
    {
        var corrections = await _reviewCorrectionService.GetPendingCorrectionsAsync(teacherId);

        var domainRejected = corrections
            .Where(c => c.Type == TeacherReviewCorrectionType.DomainQuestion
                        && !string.IsNullOrWhiteSpace(c.RejectionReason))
            .ToList();
        if (domainRejected.Count > 0)
            return BuildFixDomainVerificationStep(domainRejected);

        var hasSubjects = await _subjectRepository.HasAnySubjectOfferingsAsync(teacherId);
        var catalogDomainIds = await _domainQuestionStatusService.GetCatalogDomainIdsWithRequiredQuestionsAsync();

        if (catalogDomainIds.Count > 0)
        {
            if (await _domainQuestionStatusService.HasIncompleteCatalogDomainAnswersAsync(teacherId))
                return BuildCompleteDomainQuestionsStep();

            if (!hasSubjects)
            {
                if (await _domainQuestionStatusService.HasCatalogDomainsPendingAdminReviewAsync(teacherId))
                    return BuildAwaitingDomainVerificationStep();

                if (await _domainQuestionStatusService.AreAllCatalogDomainsFullyApprovedAsync(teacherId))
                    return BuildAddSubjectsStep();
            }
        }
        else if (!hasSubjects)
        {
            return BuildAddSubjectsStep();
        }

        if (!hasSubjects)
            return BuildAddSubjectsStep();

        return await BuildWaitingStepAsync(teacherId);
    }

    private static RegistrationStepDto BuildFixDomainVerificationStep(List<TeacherReviewCorrectionDto> corrections) =>
        new()
        {
            CurrentStep = 5,
            NextStep = 5,
            NextStepName = "Fix Domain Verification",
            IsRegistrationComplete = false,
            Message = "One or more domain verification answers were rejected. Please update them on the domain questions screen.",
            PendingCorrections = corrections
        };

    private static RegistrationStepDto BuildCompleteDomainQuestionsStep() =>
        new()
        {
            CurrentStep = 4,
            NextStep = 5,
            NextStepName = "Complete Domain Questions",
            IsRegistrationComplete = false,
            Message = "Complete the required domain verification questions for each education domain before adding teaching subjects."
        };

    private static RegistrationStepDto BuildAwaitingDomainVerificationStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 0,
            NextStepName = "Awaiting Domain Verification",
            IsRegistrationComplete = false,
            Message = "Your domain verification answers are being reviewed. You can add teaching subjects after all domains are approved."
        };

    private async Task<RegistrationStepDto> BuildWaitingStepAsync(int teacherId)
    {
        if (await _completionService.CanActivateTeacherAccountAsync(teacherId))
            return BuildAwaitingFinalApprovalStep();

        return BuildAwaitingAdminStep();
    }

    private static RegistrationStepDto BuildAddSubjectsStep() =>
        new()
        {
            CurrentStep = 4,
            NextStep = 5,
            NextStepName = "Add Teaching Subjects and Units",
            IsRegistrationComplete = false,
            Message = "Add the subjects and content units you can teach. All domain verification requirements must be approved first."
        };

    private static RegistrationStepDto BuildAwaitingAdminStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 0,
            NextStepName = "Awaiting Admin Verification",
            IsRegistrationComplete = false,
            Message = "Your documents and teaching subjects are being reviewed by our team."
        };

    private static RegistrationStepDto BuildAwaitingFinalApprovalStep() =>
        new()
        {
            CurrentStep = 5,
            NextStep = 0,
            NextStepName = "Awaiting Final Approval",
            IsRegistrationComplete = false,
            AwaitingFinalApproval = true,
            Message = "All documents and subjects are approved. Waiting for final account activation by admin."
        };

}
