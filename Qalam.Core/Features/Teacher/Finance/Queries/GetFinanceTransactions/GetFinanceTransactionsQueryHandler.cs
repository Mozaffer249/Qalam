using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactions;

public class GetFinanceTransactionsQueryHandler : ResponseHandler,
    IRequestHandler<GetFinanceTransactionsQuery, Response<PaginatedResult<TeacherFinanceTransactionDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDashboardReadRepository _dashboardRepository;

    public GetFinanceTransactionsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherDashboardReadRepository dashboardRepository) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<Response<PaginatedResult<TeacherFinanceTransactionDto>>> Handle(
        GetFinanceTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<PaginatedResult<TeacherFinanceTransactionDto>>("Teacher not found");

        var page = await _dashboardRepository.GetFinanceTransactionsAsync(
            teacher.Id,
            request.Filter,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Success(entity: page);
    }
}
