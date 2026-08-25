using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentsList;

public class GetAdminStudentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminStudentsListQuery, Response<List<AdminStudentListItemDto>>>
{
    private const int MaxPageSize = 100;

    private readonly IStudentRepository _studentRepository;

    public GetAdminStudentsListQueryHandler(
        IStudentRepository studentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Response<List<AdminStudentListItemDto>>> Handle(
        GetAdminStudentsListQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize
        };

        var filters = new AdminStudentListFilters(
            Search: request.Search,
            IsMinor: request.IsMinor,
            IsActive: request.IsActive,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _studentRepository.SearchForAdminAsync(filters, cancellationToken);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
