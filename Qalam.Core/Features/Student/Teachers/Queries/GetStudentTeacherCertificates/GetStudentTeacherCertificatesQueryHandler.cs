using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherCertificates;

public class GetStudentTeacherCertificatesQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherCertificatesQuery, Response<List<StudentTeacherCertificateDto>>>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetStudentTeacherCertificatesQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<StudentTeacherCertificateDto>>> Handle(
        GetStudentTeacherCertificatesQuery request,
        CancellationToken cancellationToken)
    {
        var certs = await _teacherRepository.GetStudentCertificatesAsync(
            request.TeacherId, request.Take, cancellationToken);

        if (certs.Count == 0)
        {
            var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
            if (teacher is null || teacher.Status != Qalam.Data.Entity.Common.Enums.TeacherStatus.Active || !teacher.IsActive)
                return NotFound<List<StudentTeacherCertificateDto>>("Teacher not found.");
        }

        return Success(entity: certs);
    }
}
