using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetEnrollmentInvoice;

public class GetEnrollmentInvoiceQueryHandler : ResponseHandler,
    IRequestHandler<GetEnrollmentInvoiceQuery, Response<TeacherEnrollmentInvoiceDto>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetEnrollmentInvoiceQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<TeacherEnrollmentInvoiceDto>> Handle(
        GetEnrollmentInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var (dto, error, forbidden) = await _enrollmentService.GetInvoiceAsync(
            request.UserId, request.Id, cancellationToken);

        if (forbidden)
            return Forbidden<TeacherEnrollmentInvoiceDto>(error ?? "Forbidden");

        if (dto == null)
            return NotFound<TeacherEnrollmentInvoiceDto>(error ?? "Enrollment not found.");

        return Success(entity: dto);
    }
}
