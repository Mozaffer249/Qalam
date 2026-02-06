using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using StudentEntity = Qalam.Data.Entity.Student.Student;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Commands.AddChild;

public class AddChildCommandHandler : ResponseHandler,
    IRequestHandler<AddChildCommand, Response<int>>
{
    private readonly IGuardianRepository _guardianRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly UserManager<User> _userManager;

    public AddChildCommandHandler(
        IGuardianRepository guardianRepository,
        IStudentRepository studentRepository,
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianRepository = guardianRepository;
        _studentRepository = studentRepository;
        _userManager = userManager;
    }

    public async Task<Response<int>> Handle(AddChildCommand request, CancellationToken cancellationToken)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian == null)
            return NotFound<int>("Guardian profile not found. Only parents can add children.");

        var dto = request.Child;
        var unique = $"child_{guardian.Id}_{Guid.NewGuid():N}"[..44];
        var childUser = new User
        {
            UserName = unique,
            FirstName = dto.FullName,
            IsActive = false
        };
        var createResult = await _userManager.CreateAsync(childUser, "TempPass1!");
        if (!createResult.Succeeded)
            return BadRequest<int>(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var student = new StudentEntity
        {
            UserId = childUser.Id,
            GuardianId = guardian.Id,
            IsMinor = true,
            GuardianRelation = dto.GuardianRelation,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            DomainId = dto.DomainId,
            CurriculumId = dto.CurriculumId,
            LevelId = dto.LevelId,
            GradeId = dto.GradeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _studentRepository.AddAsync(student);
        await _studentRepository.SaveChangesAsync();

        return Success(Message: "Child added successfully.", entity: student.Id);
    }
}
