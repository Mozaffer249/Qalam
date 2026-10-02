using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Policy;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.CancellationPolicy;

public class GetCurrentCancellationPolicyQuery : IRequest<Response<PolicyVersionDto>> { }

public class ListCancellationPolicyVersionsQuery : IRequest<Response<List<PolicyVersionDto>>> { }

public class GetCancellationPolicyVersionQuery : IRequest<Response<PolicyVersionDto>>
{
    public int Id { get; set; }
}

public class GetCancellationPolicyDraftQuery : IRequest<Response<PolicyVersionDto?>> { }

public class CompareCancellationPolicyVersionsQuery : IRequest<Response<PolicyCompareDto>>
{
    public int FromId { get; set; }
    public int ToId { get; set; }
}

public class SaveCancellationPolicyDraftCommand : IRequest<Response<PolicyVersionDto>>
{
    public int? UserId { get; set; }
    public SavePolicyDraftDto Data { get; set; } = new();
}

public class PublishCancellationPolicyDraftCommand : IRequest<Response<PolicyVersionDto>>
{
    public int? UserId { get; set; }
    public PublishPolicyDraftDto Data { get; set; } = new();
}

public class DiscardCancellationPolicyDraftCommand : IRequest<Response<bool>> { }

public class AdminCancellationPolicyHandlers : ResponseHandler,
    IRequestHandler<GetCurrentCancellationPolicyQuery, Response<PolicyVersionDto>>,
    IRequestHandler<ListCancellationPolicyVersionsQuery, Response<List<PolicyVersionDto>>>,
    IRequestHandler<GetCancellationPolicyVersionQuery, Response<PolicyVersionDto>>,
    IRequestHandler<GetCancellationPolicyDraftQuery, Response<PolicyVersionDto?>>,
    IRequestHandler<CompareCancellationPolicyVersionsQuery, Response<PolicyCompareDto>>,
    IRequestHandler<SaveCancellationPolicyDraftCommand, Response<PolicyVersionDto>>,
    IRequestHandler<PublishCancellationPolicyDraftCommand, Response<PolicyVersionDto>>,
    IRequestHandler<DiscardCancellationPolicyDraftCommand, Response<bool>>
{
    private readonly IPolicyAdminService _service;

    public AdminCancellationPolicyHandlers(IPolicyAdminService service, IStringLocalizer<SharedResources> localizer)
        : base(localizer)
    {
        _service = service;
    }

    public async Task<Response<PolicyVersionDto>> Handle(GetCurrentCancellationPolicyQuery request, CancellationToken cancellationToken)
        => Success(entity: await _service.GetCurrentAsync(cancellationToken));

    public async Task<Response<List<PolicyVersionDto>>> Handle(ListCancellationPolicyVersionsQuery request, CancellationToken cancellationToken)
        => Success(entity: await _service.ListVersionsAsync(cancellationToken));

    public async Task<Response<PolicyVersionDto>> Handle(GetCancellationPolicyVersionQuery request, CancellationToken cancellationToken)
    {
        var v = await _service.GetVersionAsync(request.Id, cancellationToken);
        return v == null ? NotFound<PolicyVersionDto>("Policy version not found.") : Success(entity: v);
    }

    public async Task<Response<PolicyVersionDto?>> Handle(GetCancellationPolicyDraftQuery request, CancellationToken cancellationToken)
        => Success(entity: await _service.GetDraftAsync(cancellationToken));

    public async Task<Response<PolicyCompareDto>> Handle(CompareCancellationPolicyVersionsQuery request, CancellationToken cancellationToken)
    {
        var diff = await _service.CompareAsync(request.FromId, request.ToId, cancellationToken);
        return diff == null ? NotFound<PolicyCompareDto>("Policy version not found.") : Success(entity: diff);
    }

    public async Task<Response<PolicyVersionDto>> Handle(SaveCancellationPolicyDraftCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveDraftAsync(request.Data, request.UserId, cancellationToken);
        return result.Succeeded
            ? Success(entity: result.Data!)
            : BadRequest<PolicyVersionDto>(string.Join(" ", result.Errors.DefaultIfEmpty(result.ErrorCode ?? "")));
    }

    public async Task<Response<PolicyVersionDto>> Handle(PublishCancellationPolicyDraftCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.PublishDraftAsync(request.Data, request.UserId, cancellationToken);
        return result.Succeeded
            ? Success(entity: result.Data!)
            : BadRequest<PolicyVersionDto>(string.Join(" ", result.Errors.DefaultIfEmpty(result.ErrorCode ?? "")));
    }

    public async Task<Response<bool>> Handle(DiscardCancellationPolicyDraftCommand request, CancellationToken cancellationToken)
        => await _service.DiscardDraftAsync(cancellationToken)
            ? Success(entity: true)
            : NotFound<bool>("There is no draft.");
}
