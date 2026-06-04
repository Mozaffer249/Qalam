using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.CreateSessionOffer;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.UpdateSessionOffer;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Commands.WithdrawSessionOffer;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetMyOfferById;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetMyOffers;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Results;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher offer management for Scenario 2: submit, update, withdraw, list, and inspect.
/// </summary>
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherSessionOffers)]
public class TeacherSessionOffersController : AppControllerBase
{
    /// <summary>
    /// Submit a new offer on a matched request. Returns 409 with `meta.existingOfferId` if the
    /// teacher already has a non-Withdrawn offer on this request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherOfferDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSessionOfferDto dto)
        => NewResult(await Mediator.Send(new CreateSessionOfferCommand { Data = dto }));

    /// <summary>
    /// Update price / notes / validity on a Pending offer. Schedules are NOT editable (the teacher
    /// implicitly accepts the student's proposed timing). Each update bumps `version` and posts a
    /// "تم تحديث العرض" system message into the chat.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeacherOfferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSessionOfferDto dto)
        => NewResult(await Mediator.Send(new UpdateSessionOfferCommand { OfferId = id, Data = dto }));

    /// <summary>Withdraw a Pending offer. A new offer can be submitted afterwards.</summary>
    [HttpPost("{id:int}/withdraw")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id, [FromBody] WithdrawSessionOfferDto? dto)
        => NewResult(await Mediator.Send(new WithdrawSessionOfferCommand
        {
            OfferId = id,
            Data = dto ?? new WithdrawSessionOfferDto()
        }));

    /// <summary>My offers list, filterable by status + date range, paginated.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PaginatedResult<TeacherOfferListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] GetMyOffersQuery query)
        => NewResult(await Mediator.Send(query));

    /// <summary>Single offer detail (with the parent request snapshot for context).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeacherOfferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
        => NewResult(await Mediator.Send(new GetMyOfferByIdQuery { OfferId = id }));
}
