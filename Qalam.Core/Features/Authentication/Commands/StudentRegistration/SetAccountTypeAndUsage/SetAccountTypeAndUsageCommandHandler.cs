using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Extensions;
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

    public SetAccountTypeAndUsageCommandHandler(
        UserManager<User> userManager,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IAuthenticationService authService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _userManager = userManager;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _authService = authService;
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

        // Create internal DTO with enum values
        var dto = new SetAccountTypeAndUsageDto
        {
            AccountType = accountType,
            UsageMode = usageMode,
            FirstName = request.Data.FirstName,
            LastName = request.Data.LastName,
            Email = request.Data.Email,
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

        // Check if requested roles are already set up
        bool studentAlreadyExists = existingStudent != null && (accountType == StudentAccountType.Student || accountType == StudentAccountType.Both);
        bool guardianAlreadyExists = existingGuardian != null && (accountType == StudentAccountType.Parent || accountType == StudentAccountType.Both);

        // If all requested roles exist, user is done
        if ((accountType == StudentAccountType.Student && studentAlreadyExists) ||
            (accountType == StudentAccountType.Parent && guardianAlreadyExists) ||
            (accountType == StudentAccountType.Both && studentAlreadyExists && guardianAlreadyExists))
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
        user.Email = dto.Email;
        user.Address = dto.CityOrRegion;
        await _userManager.UpdateAsync(user);

        if (existingStudent == null && (accountType == StudentAccountType.Student || accountType == StudentAccountType.Both))
            await _userManager.AddToRoleAsync(user, Roles.Student);
        if (existingGuardian == null && (accountType == StudentAccountType.Parent || accountType == StudentAccountType.Both))
            await _userManager.AddToRoleAsync(user, Roles.Guardian);

        var fullPhone = user.PhoneNumber ?? user.UserName ?? "";

        if (existingStudent == null && (accountType == StudentAccountType.Student || accountType == StudentAccountType.Both))
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

        if (existingGuardian == null && (accountType == StudentAccountType.Parent || accountType == StudentAccountType.Both))
        {
            await _guardianRepository.AddAsync(new GuardianEntity
            {
                UserId = user.Id,
                FullName = $"{dto.FirstName} {dto.LastName}".Trim(),
                Phone = fullPhone,
                Email = dto.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _guardianRepository.SaveChangesAsync();
        }

        // Smart logic to determine next step based on AccountType and UsageMode
        string nextStepName;
        bool isNextStepRequired;
        List<string> optionalSteps = new();
        string nextStepDescription;

        if (accountType == StudentAccountType.Student)
        {
            nextStepName = "CompleteAcademicProfile";
            isNextStepRequired = true;
            nextStepDescription = "Complete your academic profile to start.";
        }
        else if (accountType == StudentAccountType.Parent)
        {
            if (usageMode == UsageMode.StudySelf)
            {
                nextStepName = "CompleteAcademicProfile";
                isNextStepRequired = true;
                optionalSteps.Add("AddChildren");
                nextStepDescription = "Complete your academic profile. You can also add children later.";
            }
            else if (usageMode == UsageMode.AddChildren)
            {
                nextStepName = "AddChildren";
                isNextStepRequired = false;
                optionalSteps.Add("Dashboard");
                nextStepDescription = "You can add children now or skip to dashboard.";
            }
            else // UsageMode.Both
            {
                nextStepName = "CompleteAcademicProfile";
                isNextStepRequired = true;
                optionalSteps.Add("AddChildren");
                nextStepDescription = "Complete your academic profile first, then you can add children.";
            }
        }
        else // StudentAccountType.Both
        {
            nextStepName = "CompleteAcademicProfile";
            isNextStepRequired = true;
            optionalSteps.Add("AddChildren");
            nextStepDescription = "Complete your academic profile. You can add children anytime.";
        }

        var jwt = await _authService.GetJWTToken(user);
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
