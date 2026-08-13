using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Identity;
using StudentEntity = Qalam.Data.Entity.Student.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class CompleteStudentProfileCommandHandler : ResponseHandler,
    IRequestHandler<CompleteStudentProfileCommand, Response<StudentRegistrationResponseDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authService;

    public CompleteStudentProfileCommandHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        UserManager<User> userManager,
        IAuthenticationService authService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _userManager = userManager;
        _authService = authService;
    }

    public async Task<Response<StudentRegistrationResponseDto>> Handle(
        CompleteStudentProfileCommand request,
        CancellationToken cancellationToken)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        string? refreshedToken = null;

        // Self-heal accounts stuck after Parent+StudySelf/Both created Guardian only.
        if (student == null)
        {
            if (guardian == null)
                return NotFound<StudentRegistrationResponseDto>(
                    "Student profile not found. Complete registration first.");

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                return NotFound<StudentRegistrationResponseDto>("User not found.");

            student = new StudentEntity
            {
                UserId = request.UserId,
                IsMinor = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _studentRepository.AddAsync(student);
            await _studentRepository.SaveChangesAsync();

            if (!await _userManager.IsInRoleAsync(user, Roles.Student))
            {
                await _userManager.AddToRoleAsync(user, Roles.Student);
                var jwt = await _authService.GetJWTToken(user);
                refreshedToken = jwt.AccessToken;
            }
        }

        var p = request.Profile;
        student.DomainId = p.DomainId;
        student.CurriculumId = p.CurriculumId;
        student.LevelId = p.LevelId;
        student.GradeId = p.GradeId;
        student.UniversityId = p.UniversityId;
        student.CollegeId = p.CollegeId;
        student.DepartmentId = p.DepartmentId;
        student.AcademicProgramId = p.AcademicProgramId;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student);
        await _studentRepository.SaveChangesAsync();

        var optionalSteps = guardian != null ? new List<string> { "AddChildren" } : new List<string>();
        var description = guardian != null
            ? "Profile completed! You can add children or go to dashboard."
            : "Profile completed successfully!";

        var userForRoles = await _userManager.FindByIdAsync(request.UserId.ToString());
        var roles = userForRoles != null
            ? new List<string>(await _userManager.GetRolesAsync(userForRoles))
            : new List<string>();

        return Success(entity: new StudentRegistrationResponseDto
        {
            Token = refreshedToken,
            CurrentStep = 3,
            Roles = roles,
            NextStepName = "Dashboard",
            IsNextStepRequired = false,
            OptionalSteps = optionalSteps,
            NextStepDescription = description,
            IsRegistrationComplete = true,
            Message = "Academic profile saved successfully."
        });
    }
}
