using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetStudentIdentity;

public class GetStudentIdentityQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentIdentityQuery, Response<StudentIdentityResponseDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly INationalityRepository _nationalityRepository;

    public GetStudentIdentityQueryHandler(
        IStudentRepository studentRepository,
        INationalityRepository nationalityRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _nationalityRepository = nationalityRepository;
    }

    public async Task<Response<StudentIdentityResponseDto>> Handle(
        GetStudentIdentityQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<StudentIdentityResponseDto>("Student profile not found.");

        var response = new StudentIdentityResponseDto
        {
            IdentityType = student.IdentityType,
            IdentityNumber = student.IdentityNumber,
            NationalityId = student.NationalityId,
            IssuingCountryCode = student.IssuingCountryCode
        };

        if (student.NationalityId.HasValue)
        {
            var nationality = await _nationalityRepository.GetByIdAsync(student.NationalityId.Value);
            if (nationality != null)
            {
                response.NationalityCode = nationality.Code;
                response.NationalityNameAr = nationality.NameAr;
                response.NationalityNameEn = nationality.NameEn;
            }
        }

        return Success(entity: response);
    }
}
