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

    public TeacherRegistrationService(
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        IAuthenticationService authService,
        ILogger<TeacherRegistrationService> logger,
        ITeacherSubjectRepository subjectRepository,
        ITeacherAvailabilityRepository availabilityRepository,
        IAuthLoginOtpHelper authLoginOtpHelper,
        ITeacherLifecycleEmailService lifecycleEmailService)
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
                if (!await _subjectRepository.HasAnySubjectOfferingsAsync(teacher.Id))
                    return BuildAddSubjectsStep();

                return BuildAwaitingAdminStep();

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
                        RejectedDocuments = rejectedDocs
                    };
                }

                if (!await _subjectRepository.HasAnySubjectOfferingsAsync(teacher.Id))
                    return BuildAddSubjectsStep();

                return BuildAwaitingAdminStep();

            case TeacherStatus.Active:
                // Check if teacher has set availability
                if (!await _availabilityRepository.HasAnyAvailabilityAsync(teacher.Id))
                {
                    return new RegistrationStepDto
                    {
                        CurrentStep = 5,
                        NextStep = 6,
                        NextStepName = "Set Your Availability",
                        IsRegistrationComplete = false,
                        Message = "Great! Now set your weekly availability so students can book sessions with you."
                    };
                }

                // All setup complete
                return new RegistrationStepDto
                {
                    CurrentStep = 6,
                    NextStep = 0,
                    NextStepName = "Registration Complete",
                    IsRegistrationComplete = true,
                    Message = "Your profile is complete! You can now start accepting students."
                };

            case TeacherStatus.Blocked:
                throw new Exception("Your account has been blocked. Please contact support.");

            default:
                throw new Exception("Unknown registration status");
        }
    }

    private static RegistrationStepDto BuildAddSubjectsStep() =>
        new()
        {
            CurrentStep = 4,
            NextStep = 5,
            NextStepName = "Add Teaching Subjects and Units",
            IsRegistrationComplete = false,
            Message = "Add the subjects and content units you can teach. An admin will review them with your certificates."
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

}
