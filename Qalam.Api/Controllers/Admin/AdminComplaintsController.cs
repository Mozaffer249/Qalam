using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Complaints")]
public class AdminComplaintsController : AppControllerBase
{
    private readonly IComplaintService _complaints;

    public AdminComplaintsController(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    [HttpGet(Router.AdminComplaints)]
    [ProducesResponseType(typeof(List<ComplaintListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] ComplaintSubjectType? subjectType,
        [FromQuery] ComplaintPriority? priority,
        [FromQuery] int? assignedToUserId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? scope,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ComplaintListFilter
        {
            Status = status,
            SubjectType = subjectType,
            Priority = priority,
            AssignedToUserId = assignedToUserId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Scope = scope,
        };
        var (items, total) = await _complaints.ListAsync(filter, pageNumber, pageSize, "Admin", cancellationToken);
        return NewResult(OkResponse(items, new { total, pageNumber, pageSize }));
    }

    [HttpGet(Router.AdminComplaintCounts)]
    [ProducesResponseType(typeof(ComplaintCountsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Counts(
        [FromQuery] ComplaintSubjectType? subjectType,
        CancellationToken cancellationToken = default)
    {
        var counts = await _complaints.GetCountsAsync(
            new ComplaintListFilter { SubjectType = subjectType },
            cancellationToken);
        return NewResult(OkResponse(counts));
    }

    [HttpGet(Router.AdminComplaintById)]
    [ProducesResponseType(typeof(ComplaintDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _complaints.GetAsync(id, CurrentUserId(), "Admin", cancellationToken: cancellationToken);
        if (detail == null)
            return NewResult(NotFoundResponse<ComplaintDetailDto>("Complaint not found."));
        return NewResult(OkResponse(detail));
    }

    [HttpPost(Router.AdminComplaintAssign)]
    public async Task<IActionResult> Assign(
        int id,
        [FromBody] AssignComplaintBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.AssignAsync(id, CurrentUserId() ?? 0, body.AssignedToUserId, cancellationToken);
            return NewResult(OkResponse("Complaint assigned."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    [HttpPost(Router.AdminComplaintPriority)]
    public async Task<IActionResult> SetPriority(
        int id,
        [FromBody] SetPriorityBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.SetPriorityAsync(id, CurrentUserId() ?? 0, body.Priority, cancellationToken);
            return NewResult(OkResponse("Priority updated."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    [HttpPost(Router.AdminComplaintRequestInfo)]
    public async Task<IActionResult> RequestInfo(
        int id,
        [FromBody] RequestInfoBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.RequestInfoAsync(id, CurrentUserId() ?? 0, body.Target, body.Notes, cancellationToken);
            return NewResult(OkResponse("Info requested."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    [HttpPost(Router.AdminComplaintAdvance)]
    public async Task<IActionResult> Advance(
        int id,
        [FromBody] AdvanceBody? body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.AdvanceAsync(id, CurrentUserId() ?? 0, body?.ToStatus, cancellationToken);
            return NewResult(OkResponse("Complaint advanced."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    [HttpGet(Router.AdminComplaintResolvePreview)]
    public async Task<IActionResult> ResolvePreview(
        int id,
        [FromQuery] ComplaintResolution resolutionCode,
        [FromQuery] decimal? refundAmount,
        [FromQuery] int? paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var preview = await _complaints.GetResolvePreviewAsync(
                id, resolutionCode, refundAmount, paymentId, cancellationToken);
            return NewResult(OkResponse(preview));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<ComplaintResolvePreviewDto>(ex.Message));
        }
    }

    [HttpPost(Router.AdminComplaintResolve)]
    public async Task<IActionResult> Resolve(
        int id,
        [FromBody] ResolveComplaintRequest body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.ResolveAsync(id, CurrentUserId() ?? 0, body, cancellationToken);
            return NewResult(OkResponse("Complaint resolved."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    [HttpPost(Router.AdminComplaintCancel)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.CancelAsync(id, CurrentUserId() ?? 0, body.Notes, cancellationToken);
            return NewResult(OkResponse("Complaint cancelled."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    private int? CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static Response<T> OkResponse<T>(T data, object? meta = null) => new()
    {
        Succeeded = true,
        StatusCode = System.Net.HttpStatusCode.OK,
        Data = data,
        Meta = meta,
        Message = "Success",
    };

    private static Response<T> BadRequestResponse<T>(string message) => new()
    {
        Succeeded = false,
        StatusCode = System.Net.HttpStatusCode.BadRequest,
        Message = message,
    };

    private static Response<T> NotFoundResponse<T>(string message) => new()
    {
        Succeeded = false,
        StatusCode = System.Net.HttpStatusCode.NotFound,
        Message = message,
    };

    public sealed class AssignComplaintBody
    {
        public int AssignedToUserId { get; set; }
    }

    public sealed class SetPriorityBody
    {
        public ComplaintPriority Priority { get; set; }
    }

    public sealed class RequestInfoBody
    {
        public string Target { get; set; } = "Respondent";
        public string? Notes { get; set; }
    }

    public sealed class AdvanceBody
    {
        public ComplaintStatus? ToStatus { get; set; }
    }

    public sealed class CancelBody
    {
        public string Notes { get; set; } = "";
    }
}
