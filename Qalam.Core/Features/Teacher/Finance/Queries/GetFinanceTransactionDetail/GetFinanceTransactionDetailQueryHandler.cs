using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactionDetail;

public class GetFinanceTransactionDetailQueryHandler : ResponseHandler,
    IRequestHandler<GetFinanceTransactionDetailQuery, Response<TeacherFinanceTransactionDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherFinanceDetailService _financeDetailService;

    public GetFinanceTransactionDetailQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherFinanceDetailService financeDetailService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _financeDetailService = financeDetailService;
    }

    public async Task<Response<TeacherFinanceTransactionDetailDto>> Handle(
        GetFinanceTransactionDetailQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherFinanceTransactionDetailDto>("Teacher not found");

        var detail = await _financeDetailService.GetTransactionDetailAsync(
            teacher.Id,
            request.TransactionId,
            cancellationToken);

        if (detail == null)
            return NotFound<TeacherFinanceTransactionDetailDto>("Transaction not found");

        return Success(entity: detail);
    }
}
