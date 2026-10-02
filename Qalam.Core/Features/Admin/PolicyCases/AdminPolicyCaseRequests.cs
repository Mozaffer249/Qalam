using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Policy;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Core.Features.Admin.PolicyCases;

public class ListPolicyCasesQuery : IRequest<Response<PolicyCaseListResultDto>>
{
    public PolicyCaseListFilter Filter { get; set; } = new();
}

public class GetPolicyCaseQuery : IRequest<Response<PolicyCaseDetailDto>>
{
    public int Id { get; set; }
}

public class ApplyPolicyExceptionCommand : IRequest<Response<PolicyCaseDetailDto>>
{
    public int? UserId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public AdminPolicyExceptionRequest Data { get; set; } = new();
}

public class ReversePolicyCaseCommand : IRequest<Response<PolicyCaseDetailDto>>
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Reason { get; set; } = "";
}

public class GetEnrollmentFinancialTimelineQuery : IRequest<Response<List<FinancialTimelineEntryDto>>>
{
    public int EnrollmentId { get; set; }
}

public class AdminPolicyCaseHandlers : ResponseHandler,
    IRequestHandler<ListPolicyCasesQuery, Response<PolicyCaseListResultDto>>,
    IRequestHandler<GetPolicyCaseQuery, Response<PolicyCaseDetailDto>>,
    IRequestHandler<ApplyPolicyExceptionCommand, Response<PolicyCaseDetailDto>>,
    IRequestHandler<ReversePolicyCaseCommand, Response<PolicyCaseDetailDto>>,
    IRequestHandler<GetEnrollmentFinancialTimelineQuery, Response<List<FinancialTimelineEntryDto>>>
{
    private readonly IPolicyCaseAdminService _service;

    public AdminPolicyCaseHandlers(IPolicyCaseAdminService service, IStringLocalizer<SharedResources> localizer)
        : base(localizer)
    {
        _service = service;
    }

    public async Task<Response<PolicyCaseListResultDto>> Handle(ListPolicyCasesQuery request, CancellationToken cancellationToken)
        => Success(entity: await _service.ListAsync(request.Filter, cancellationToken));

    public async Task<Response<PolicyCaseDetailDto>> Handle(GetPolicyCaseQuery request, CancellationToken cancellationToken)
    {
        var dto = await _service.GetAsync(request.Id, cancellationToken);
        return dto == null ? NotFound<PolicyCaseDetailDto>("Policy case not found.") : Success(entity: dto);
    }

    public async Task<Response<PolicyCaseDetailDto>> Handle(ApplyPolicyExceptionCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId is not int userId)
            return Unauthorized<PolicyCaseDetailDto>();
        try
        {
            return Success(entity: await _service.ApplyExceptionAsync(request.Data, userId, request.IsSuperAdmin, cancellationToken));
        }
        catch (PolicyDeniedException ex)
        {
            return BadRequest<PolicyCaseDetailDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PolicyCaseDetailDto>(ex.Message);
        }
    }

    public async Task<Response<PolicyCaseDetailDto>> Handle(ReversePolicyCaseCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId is not int userId)
            return Unauthorized<PolicyCaseDetailDto>();
        try
        {
            var dto = await _service.ReverseAsync(request.Id, request.Reason, userId, cancellationToken);
            return dto == null ? NotFound<PolicyCaseDetailDto>("Policy case not found.") : Success(entity: dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PolicyCaseDetailDto>(ex.Message);
        }
    }

    public async Task<Response<List<FinancialTimelineEntryDto>>> Handle(GetEnrollmentFinancialTimelineQuery request, CancellationToken cancellationToken)
    {
        var timeline = await _service.GetEnrollmentTimelineAsync(request.EnrollmentId, cancellationToken);
        return timeline == null
            ? NotFound<List<FinancialTimelineEntryDto>>("Enrollment not found.")
            : Success(entity: timeline);
    }
}
