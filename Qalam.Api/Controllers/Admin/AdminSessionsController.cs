using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Sessions.Commands.AssignSessionComplaint;
using Qalam.Core.Features.Admin.Sessions.Commands.BlockAdminSessionTeacher;
using Qalam.Core.Features.Admin.Sessions.Commands.CancelAdminSession;
using Qalam.Core.Features.Admin.Sessions.Commands.HoldAdminSessionEarning;
using Qalam.Core.Features.Admin.Sessions.Commands.IssueAdminSessionRefund;
using Qalam.Core.Features.Admin.Sessions.Commands.ReleaseAdminSessionEarning;
using Qalam.Core.Features.Admin.Sessions.Commands.RequestTeacherComplaintResponse;
using Qalam.Core.Features.Admin.Sessions.Commands.ResolveSessionComplaint;
using Qalam.Core.Features.Admin.Sessions.Commands.SetAdminSessionAttendance;
using Qalam.Core.Features.Admin.Sessions.Commands.VoidAdminSessionEarning;
using Qalam.Core.Features.Admin.Sessions.Commands.WarnAdminSessionTeacher;
using Qalam.Core.Features.Admin.Sessions.Queries.GetAdminSessionById;
using Qalam.Core.Features.Admin.Sessions.Queries.GetComplaintResolvePreview;
using Qalam.Core.Features.Admin.Sessions.Queries.ListAdminSessions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Sessions")]
public class AdminSessionsController : AppControllerBase
{
    [HttpGet(Router.AdminSessions)]
    [ProducesResponseType(typeof(List<AdminSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] AdminSessionListFilter filter,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ListAdminSessionsQuery
        {
            Status = filter.Status,
            TeacherId = filter.TeacherId,
            StudentId = filter.StudentId,
            EnrollmentId = filter.EnrollmentId,
            HasComplaint = filter.HasComplaint,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
        }, cancellationToken));

    [HttpGet(Router.AdminSessionById)]
    [ProducesResponseType(typeof(AdminSessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminSessionByIdQuery { Id = id }, cancellationToken));

    [HttpPost(Router.AdminSessionAttendance)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAttendance(
        int id,
        [FromBody] AdminSetSessionAttendanceRequest body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new SetAdminSessionAttendanceCommand
        {
            ScheduleId = id,
            Body = body,
        }, cancellationToken));

    [HttpPost(Router.AdminSessionCancel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new CancelAdminSessionCommand { ScheduleId = id }, cancellationToken));

    [HttpPost(Router.AdminSessionRefund)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(
        int id,
        [FromBody] AdminSessionRefundRequest body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new IssueAdminSessionRefundCommand
        {
            ScheduleId = id,
            Body = body,
        }, cancellationToken));

    [HttpPost(Router.AdminSessionEarningHold)]
    public async Task<IActionResult> HoldEarning(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new HoldAdminSessionEarningCommand { ScheduleId = id }, cancellationToken));

    [HttpPost(Router.AdminSessionEarningRelease)]
    public async Task<IActionResult> ReleaseEarning(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ReleaseAdminSessionEarningCommand { ScheduleId = id }, cancellationToken));

    [HttpPost(Router.AdminSessionEarningVoid)]
    public async Task<IActionResult> VoidEarning(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new VoidAdminSessionEarningCommand { ScheduleId = id }, cancellationToken));

    [HttpPost(Router.AdminSessionWarnTeacher)]
    public async Task<IActionResult> WarnTeacher(
        int id,
        [FromBody] string? notes,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new WarnAdminSessionTeacherCommand
        {
            ScheduleId = id,
            Notes = notes,
        }, cancellationToken));

    [HttpPost(Router.AdminSessionBlockTeacher)]
    public async Task<IActionResult> BlockTeacher(int id, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new BlockAdminSessionTeacherCommand { ScheduleId = id }, cancellationToken));

    [HttpPost(Router.AdminSessionComplaintAssign)]
    public async Task<IActionResult> AssignComplaint(
        int id,
        int complaintId,
        [FromBody] int assignedToUserId,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new AssignSessionComplaintCommand
        {
            ScheduleId = id,
            ComplaintId = complaintId,
            AssignedToUserId = assignedToUserId,
        }, cancellationToken));

    [HttpPost(Router.AdminSessionComplaintRequestTeacher)]
    public async Task<IActionResult> RequestTeacherResponse(
        int id,
        int complaintId,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new RequestTeacherComplaintResponseCommand
        {
            ScheduleId = id,
            ComplaintId = complaintId,
        }, cancellationToken));

    [HttpGet(Router.AdminSessionComplaintResolvePreview)]
    [ProducesResponseType(typeof(ComplaintResolvePreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplaintResolvePreview(
        int id,
        int complaintId,
        [FromQuery] SessionComplaintResolution resolutionCode,
        [FromQuery] decimal? refundAmount = null,
        [FromQuery] int? paymentId = null,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetComplaintResolvePreviewQuery
        {
            ScheduleId = id,
            ComplaintId = complaintId,
            ResolutionCode = resolutionCode,
            RefundAmount = refundAmount,
            PaymentId = paymentId,
        }, cancellationToken));

    [HttpPost(Router.AdminSessionComplaintResolve)]
    public async Task<IActionResult> ResolveComplaint(
        int id,
        int complaintId,
        [FromBody] ResolveSessionComplaintRequest body,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new ResolveSessionComplaintCommand
        {
            ScheduleId = id,
            ComplaintId = complaintId,
            Body = body,
        }, cancellationToken));
}
