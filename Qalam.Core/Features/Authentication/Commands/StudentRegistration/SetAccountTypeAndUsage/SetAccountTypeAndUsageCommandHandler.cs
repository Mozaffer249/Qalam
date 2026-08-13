using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Extensions;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Identity;
using StudentEntity = Qalam.Data.Entity.Student.Student;
using GuardianEntity = Qalam.Data.Entity.Student.Guardian;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class SetAccountTypeAndUsageCommandHandler : ResponseHandler,
    IRequestHandler<SetAccountTypeAndUsageCommand, Response<StudentRegistrationResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILegalConsentService _consentService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SetAccountTypeAndUsageCommandHandler(
        UserManager<User> userManager,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IAuthenticationService authService,
        ILegalConsentService consentService,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _userManager = userManager;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _authService = authService;
        _consentService = consentService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<StudentRegistrationResponseDto>> Handle(
        SetAccountTypeAndUsageCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return NotFound<StudentRegistrationResponseDto>("User not found.");

        // Convert string to enum
        var accountType = request.Data.AccountType.ToStudentAccountType();
        var usageMode = !string.IsNullOrEmpty(request.Data.UsageMode)
            ? request.Data.UsageMode.ToUsageMode()
            : (UsageMode?)null;

        var resolvedEmail = !string.IsNullOrWhiteSpace(request.Data.Email)
            ? request.Data.Email.Trim()
            : user.Email;

        // Create internal DTO with enum values
        var dto = new SetAccountTypeAndUsageDto
        {
            AccountType = accountType,
            UsageMode = usageMode,
            FirstName = request.Data.FirstName,
            LastName = request.Data.LastName,
            Email = resolvedEmail ?? string.Empty,
            Password = request.Data.Password,
            CityOrRegion = request.Data.CityOrRegion,
            DateOfBirth = request.Data.DateOfBirth
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dto.DateOfBirth.Year;
        if (dto.DateOfBirth > today.AddYears(-age)) age--;
        if (age < 18)
            return BadRequest<StudentRegistrationResponseDto>("You must be 18 years or older to register.");

        var existingStudent = await _studentRepository.GetByUserIdAsync(user.Id);
        var existingGuardian = await _guardianRepository.GetByUserIdAsync(user.Id);

        // Parent+StudySelf/Both also needs a self Student (same user, GuardianId null).
        var needsSelfStudent =
            accountType == StudentAccountType.Student
            || accountType == StudentAccountType.Both
            || (accountType == StudentAccountType.Parent
                && usageMode is UsageMode.StudySelf or UsageMode.Both);

        var needsGuardian =
            accountType == StudentAccountType.Parent
            || accountType == StudentAccountType.Both;

        // If all requested roles/entities exist, user is done
        var studentAlreadyExists = existingStudent != null && needsSelfStudent;
        var guardianAlreadyExists = existingGuardian != null && needsGuardian;
        if ((!needsSelfStudent || studentAlreadyExists) && (!needsGuardian || guardianAlreadyExists)
            && (needsSelfStudent || needsGuardian))
        {
            // Regenerate token to include all current roles
            var jwtToken = await _authService.GetJWTToken(user);
            return Success(entity: new StudentRegistrationResponseDto
            {
                Token = jwtToken.AccessToken,
                CurrentStep = 1,
                NextStepName = "Dashboard",
                IsNextStepRequired = false,
                OptionalSteps = new List<string>(),
                NextStepDescription = "You're all set!",
                IsRegistrationComplete = true,
                Message = "Account already set up with requested roles."
            });
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var trimmedEmail = dto.Email.Trim();
            if (!string.IsNullOrEmpty(user.Email) &&
                !string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest<StudentRegistrationResponseDto>("Email does not match the address used during verification.");
            }

            // Only run the collision check when we're assigning an email this user doesn't already have.
            if (string.IsNullOrEmpty(user.Email))
            {
                User? emailOwner;
                try
                {
                    emailOwner = await _userManager.FindByEmailAsync(trimmedEmail);
                }
                catch (InvalidOperationException)
                {
                    // Legacy duplicates already in the DB — treat as a hard collision.
                    return BadRequest<StudentRegistrationResponseDto>("Email is already registered.");
                }
                if (emailOwner != null && emailOwner.Id != user.Id)
                {
                    return BadRequest<StudentRegistrationResponseDto>("Email is already registered.");
                }
            }

            user.Email = trimmedEmail;
            user.NormalizedEmail = trimmedEmail.ToUpperInvariant();
        }
        else if (string.IsNullOrEmpty(user.Email))
        {
            return BadRequest<StudentRegistrationResponseDto>("Email is required.");
        }

        user.Address = dto.CityOrRegion;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            // Identity's RequireUniqueEmail validator surfaces here if the chosen email
            // collides with another user. Pass the message through unchanged.
            return BadRequest<StudentRegistrationResponseDto>(
                string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        if (existingStudent == null && needsSelfStudent)
            await _userManager.AddToRoleAsync(user, Roles.Student);
        if (existingGuardian == null && needsGuardian)
            await _userManager.AddToRoleAsync(user, Roles.Guardian);

        var fullPhone = user.PhoneNumber ?? user.UserName ?? "";

        if (existingStudent == null && needsSelfStudent)
        {
            await _studentRepository.AddAsync(new StudentEntity
            {
                UserId = user.Id,
                DateOfBirth = dto.DateOfBirth,
                IsMinor = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _studentRepository.SaveChangesAsync();
        }

        if (existingGuardian == null && needsGuardian)
        {
            await _guardianRepository.AddAsync(new GuardianEntity
            {
                UserId = user.Id,
                FullName = $"{dto.FirstName} {dto.LastName}".Trim(),
                Phone = fullPhone,
                Email = user.Email ?? dto.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _guardianRepository.SaveChangesAsync();
        }

        // Next step from intent: study → academic; children-only → dashboard (AddChildren optional)
        string nextStepName;
        bool isNextStepRequired;
        List<string> optionalSteps = new();
        string nextStepDescription;

        if (needsSelfStudent)
        {
            nextStepName = "CompleteAcademicProfile";
            isNextStepRequired = true;
            if (needsGuardian)
            {
                optionalSteps.Add("AddChildren");
                nextStepDescription = "Complete your academic profile first, then you can add children.";
            }
            else
            {
                nextStepDescription = "Complete your academic profile to start.";
            }
        }
        else
        {
            // Parent + AddChildren: guardian-only, no academic gate
            nextStepName = "Dashboard";
            isNextStepRequired = false;
            optionalSteps.Add("AddChildren");
            nextStepDescription = "You can add children from home anytime.";
        }

        var jwt = await _authService.GetJWTToken(user);

        if (request.Data.AcceptedTerms)
        {
            var ctx = _httpContextAccessor.HttpContext;
            await _consentService.AcceptRequiredAsync(
                user.Id,
                source: "student-register",
                ipAddress: ClientIpHelper.GetClientIpAddress(ctx),
                userAgent: ClientIpHelper.GetUserAgent(ctx),
                cancellationToken);
        }

        return Success(entity: new StudentRegistrationResponseDto
        {
            Token = jwt.AccessToken,
            CurrentStep = 2,
            NextStepName = nextStepName,
            IsNextStepRequired = isNextStepRequired,
            OptionalSteps = optionalSteps,
            NextStepDescription = nextStepDescription,
            IsRegistrationComplete = false,
            Message = "Account type set successfully."
        });
    }
}
