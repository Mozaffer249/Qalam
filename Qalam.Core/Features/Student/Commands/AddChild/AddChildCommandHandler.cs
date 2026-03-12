using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ICurriculumRepository _curriculumRepository;
    private readonly IEducationLevelRepository _levelRepository;
    private readonly IGradeRepository _gradeRepository;

    public AddChildCommandHandler(
        IGuardianRepository guardianRepository,
        IStudentRepository studentRepository,
        UserManager<User> userManager,
        IMapper mapper,
        IEducationDomainRepository domainRepository,
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianRepository = guardianRepository;
        _studentRepository = studentRepository;
        _userManager = userManager;
        _mapper = mapper;
        _domainRepository = domainRepository;
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
    }

    public async Task<Response<int>> Handle(AddChildCommand request, CancellationToken cancellationToken)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian == null)
            return NotFound<int>("Guardian profile not found. Only parents can add children.");

        var dto = request.Child;

        // Email uniqueness check
        User? existingByEmail;
        try
        {
            existingByEmail = await _userManager.FindByEmailAsync(dto.Email?.Trim() ?? "");
        }
        catch (InvalidOperationException)
        {
            return BadRequest<int>("Email is already registered. Please use a different email or contact support.");
        }
        if (existingByEmail != null)
            return BadRequest<int>("Email is already registered.");

        // Education entity existence checks
        if (dto.DomainId.HasValue)
        {
            var domain = await _domainRepository.GetByIdAsync(dto.DomainId.Value);
            if (domain == null)
                return BadRequest<int>("Domain not found.");
        }

        if (dto.CurriculumId.HasValue)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(dto.CurriculumId.Value);
            if (curriculum == null)
                return BadRequest<int>("Curriculum not found.");
        }

        if (dto.LevelId.HasValue)
        {
            var level = await _levelRepository.GetByIdAsync(dto.LevelId.Value);
            if (level == null)
                return BadRequest<int>("Level not found.");
        }

        if (dto.GradeId.HasValue)
        {
            var grade = await _gradeRepository.GetByIdAsync(dto.GradeId.Value);
            if (grade == null)
                return BadRequest<int>("Grade not found.");
        }

        var rawUserName = $"child_{guardian.Id}_{Guid.NewGuid():N}";
        var unique = rawUserName.Length > 44 ? rawUserName[..44] : rawUserName;
        var childUser = new User
        {
            UserName = unique,
            FirstName = dto.FullName?.Trim() ?? "",
            Email = dto.Email?.Trim() ?? "",
            EmailConfirmed = true,
            IsActive = true
        };
        var createResult = await _userManager.CreateAsync(childUser, request.Child.Password);
        if (!createResult.Succeeded)
            return BadRequest<int>(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var student = _mapper.Map<StudentEntity>(dto);
        student.UserId = childUser.Id;
        student.GuardianId = guardian.Id;

        await _studentRepository.AddAsync(student);
        await _studentRepository.SaveChangesAsync();

        return Success(Message: "Child added successfully.", entity: student.Id);
    }
}
