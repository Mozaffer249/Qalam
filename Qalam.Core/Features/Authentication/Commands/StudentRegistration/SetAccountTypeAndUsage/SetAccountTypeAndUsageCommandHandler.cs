using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
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

        var dto = request.Data;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dto.DateOfBirth.Year;
        if (dto.DateOfBirth > today.AddYears(-age)) age--;
        if (age < 18)
            return BadRequest<StudentRegistrationResponseDto>("You must be 18 years or older to register.");

        var existingStudent = await _studentRepository.GetByUserIdAsync(user.Id);
        var existingGuardian = await _guardianRepository.GetByUserIdAsync(user.Id);
        if (existingStudent != null && existingGuardian != null)
            return Success(entity: new StudentRegistrationResponseDto
            {
                CurrentStep = 1,
                NextStepName = "Dashboard",
                IsRegistrationComplete = true,
                Message = "Account already set up."
            });

        if (!string.IsNullOrEmpty(dto.PasswordSetupToken))
        {
            var resetResult = await _userManager.ResetPasswordAsync(user, dto.PasswordSetupToken, dto.Password);
            if (!resetResult.Succeeded)
                return BadRequest<StudentRegistrationResponseDto>(
                    string.Join("; ", resetResult.Errors.Select(e => e.Description)));
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Address = dto.CityOrRegion;
        await _userManager.UpdateAsync(user);

        var accountType = dto.AccountType;
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

        string nextStepName = accountType == StudentAccountType.Parent ? "AddChildren" : "CompleteAcademicProfile";
        if (accountType == StudentAccountType.Both) nextStepName = "CompleteAcademicProfile";

        var jwt = await _authService.GetJWTToken(user);
        return Success(entity: new StudentRegistrationResponseDto
        {
            Token = jwt.AccessToken,
            CurrentStep = 2,
            NextStepName = nextStepName,
            IsRegistrationComplete = false,
            Message = "Account type set. Complete your profile or add children."
        });
    }
}
