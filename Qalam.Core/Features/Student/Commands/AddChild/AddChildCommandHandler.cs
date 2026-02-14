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

    public AddChildCommandHandler(
        IGuardianRepository guardianRepository,
        IStudentRepository studentRepository,
        UserManager<User> userManager,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianRepository = guardianRepository;
        _studentRepository = studentRepository;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<Response<int>> Handle(AddChildCommand request, CancellationToken cancellationToken)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian == null)
            return NotFound<int>("Guardian profile not found. Only parents can add children.");

        var dto = request.Child;

        User? existingByEmail;
        try
        {
            existingByEmail = await _userManager.FindByEmailAsync(dto.Email?.Trim() ?? "");
        }
        catch (InvalidOperationException)
        {
            // Identity uses SingleOrDefault; duplicate emails in DB cause "Sequence contains more than one element"
            return BadRequest<int>("Email is already registered. Please use a different email or contact support.");
        }
        if (existingByEmail != null)
            return BadRequest<int>("Email is already registered.");

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
