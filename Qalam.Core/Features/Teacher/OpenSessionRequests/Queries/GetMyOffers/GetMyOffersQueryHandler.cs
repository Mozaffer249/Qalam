using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetMyOffers;

public class GetMyOffersQueryHandler : ResponseHandler,
    IRequestHandler<GetMyOffersQuery, Response<PaginatedResult<TeacherOfferListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionOfferRepository _offerRepo;

    public GetMyOffersQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionOfferRepository offerRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _offerRepo = offerRepo;
    }

    public async Task<Response<PaginatedResult<TeacherOfferListItemDto>>> Handle(GetMyOffersQuery request, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<PaginatedResult<TeacherOfferListItemDto>>("Teacher account not active.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var pageNumber = Math.Max(1, request.PageNumber);

        var filters = new TeacherMyOffersFilters(request.Status, request.DateFrom, request.DateTo, pageNumber, pageSize);
        var page = await _offerRepo.GetTeacherMyOffersAsync(teacher.Id, filters, cancellationToken);
        return Success(entity: page);
    }
}
