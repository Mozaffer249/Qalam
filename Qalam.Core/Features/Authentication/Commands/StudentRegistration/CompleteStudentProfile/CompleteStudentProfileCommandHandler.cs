using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class CompleteStudentProfileCommandHandler : ResponseHandler,
    IRequestHandler<CompleteStudentProfileCommand, Response<StudentRegistrationResponseDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;

    public CompleteStudentProfileCommandHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
    }

    public async Task<Response<StudentRegistrationResponseDto>> Handle(
        CompleteStudentProfileCommand request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<StudentRegistrationResponseDto>("Student profile not found. Complete registration first.");

        var p = request.Profile;
        student.DomainId = p.DomainId;
        student.CurriculumId = p.CurriculumId;
        student.LevelId = p.LevelId;
        student.GradeId = p.GradeId;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student);
        await _studentRepository.SaveChangesAsync();

        // Check if user also has Guardian role
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        var optionalSteps = guardian != null ? new List<string> { "AddChildren" } : new List<string>();
        var description = guardian != null 
            ? "Profile completed! You can add children or go to dashboard." 
            : "Profile completed successfully!";

        return Success(entity: new StudentRegistrationResponseDto
        {
            CurrentStep = 3,
            NextStepName = "Dashboard",
            IsNextStepRequired = false,
            OptionalSteps = optionalSteps,
            NextStepDescription = description,
            IsRegistrationComplete = true,
            Message = "Academic profile saved successfully."
        });
    }
}
