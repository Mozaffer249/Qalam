using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentById;

public class GetAdminStudentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminStudentByIdQuery, Response<AdminStudentDetailDto?>>
{
    private readonly IStudentRepository _studentRepository;

    public GetAdminStudentByIdQueryHandler(
        IStudentRepository studentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Response<AdminStudentDetailDto?>> Handle(
        GetAdminStudentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _studentRepository.GetAdminDetailAsync(request.StudentId, cancellationToken);
        if (detail == null)
            return NotFound<AdminStudentDetailDto?>("Student not found");

        return Success<AdminStudentDetailDto?>(entity: detail);
    }
}
