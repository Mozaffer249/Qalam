using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.Validators;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Core.Features.Student.Commands.SetStudentIdentity;

public class SetStudentIdentityCommandHandler : ResponseHandler,
    IRequestHandler<SetStudentIdentityCommand, Response<StudentIdentityResponseDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly INationalityRepository _nationalityRepository;
    private readonly IStudentIdentityService _identityService;

    public SetStudentIdentityCommandHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        INationalityRepository nationalityRepository,
        IStudentIdentityService identityService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _nationalityRepository = nationalityRepository;
        _identityService = identityService;
    }

    public async Task<Response<StudentIdentityResponseDto>> Handle(
        SetStudentIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Identity;

        StudentEntity? student;
        if (request.TargetStudentId.HasValue)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null)
                return NotFound<StudentIdentityResponseDto>("Guardian profile not found.");

            student = await _studentRepository.GetByIdAsync(request.TargetStudentId.Value);
            if (student == null || student.GuardianId != guardian.Id)
                return NotFound<StudentIdentityResponseDto>("Child not found or does not belong to this guardian.");
        }
        else
        {
            student = await _studentRepository.GetByUserIdAsync(request.UserId);
            if (student == null)
                return NotFound<StudentIdentityResponseDto>("Student profile not found.");
        }

        var nationality = await _nationalityRepository.GetByIdAsync(dto.NationalityId);
        if (nationality == null || !nationality.IsActive)
            return BadRequest<StudentIdentityResponseDto>("Nationality not found or inactive.");

        var allowedTypes = await _identityService.GetAllowedIdentityTypesAsync(dto.NationalityId, cancellationToken);
        if (allowedTypes.Count == 0)
            return BadRequest<StudentIdentityResponseDto>("No identity types are configured for this nationality.");

        StudentIdentityBusinessRules.ValidateIdentityTypeAllowed(dto.IdentityType, allowedTypes);

        var rule = await _identityService.GetRuleForNationalityAsync(dto.NationalityId, cancellationToken);
        StudentIdentityBusinessRules.ValidateIssuingCountryRules(
            dto.IdentityType, dto.IssuingCountryCode, rule?.IssuingCountryRequired ?? false);

        await StudentIdentityBusinessRules.ValidateIdentityUnique(
            _studentRepository, dto.IdentityType, dto.IdentityNumber, dto.IssuingCountryCode, student.Id);

        student.NationalityId = dto.NationalityId;
        student.IdentityType = dto.IdentityType;
        student.IdentityNumber = dto.IdentityNumber.Trim();
        student.IssuingCountryCode = string.IsNullOrWhiteSpace(dto.IssuingCountryCode)
            ? null
            : dto.IssuingCountryCode.Trim().ToUpperInvariant();

        await _studentRepository.UpdateAsync(student);
        await _studentRepository.SaveChangesAsync();

        return Success(entity: new StudentIdentityResponseDto
        {
            IdentityType = student.IdentityType,
            IdentityNumber = student.IdentityNumber,
            NationalityId = student.NationalityId,
            NationalityCode = nationality.Code,
            NationalityNameAr = nationality.NameAr,
            NationalityNameEn = nationality.NameEn,
            IssuingCountryCode = student.IssuingCountryCode
        });
    }
}
