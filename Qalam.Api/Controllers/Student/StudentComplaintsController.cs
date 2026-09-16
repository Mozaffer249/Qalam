using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Student;

[ApiController]
[Authorize(Roles = "Student,Guardian")]
[Tags("Student · Complaints")]
public class StudentComplaintsController : AppControllerBase
{
    private readonly IComplaintService _complaints;

    public StudentComplaintsController(IComplaintService complaints)
    {
        _complaints = complaints;
    }

    [HttpGet(Router.StudentComplaints)]
    public async Task<IActionResult> List(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] ComplaintSubjectType? subjectType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var role = ResolveRole();
        var filter = new ComplaintListFilter
        {
            Status = status,
            SubjectType = subjectType,
            ComplainantUserId = userId,
        };
        var (items, total) = await _complaints.ListAsync(filter, pageNumber, pageSize, role.ToString(), cancellationToken);
        return NewResult(OkResponse(items, new { total, pageNumber, pageSize }));
    }

    [HttpGet(Router.StudentComplaintById)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var role = ResolveRole();
        var detail = await _complaints.GetAsync(id, userId, role.ToString(), cancellationToken: cancellationToken);
        if (detail == null)
            return NewResult(NotFoundResponse<ComplaintDetailDto>("Complaint not found."));
        return NewResult(OkResponse(detail));
    }

    [HttpPost(Router.StudentComplaints)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> File(
        [FromForm] FileComplaintForm form,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = CurrentUserId();
            var role = ResolveRole();
            var request = new FileComplaintRequest
            {
                SubjectType = form.SubjectType,
                CourseScheduleId = form.CourseScheduleId,
                EnrollmentId = form.EnrollmentId,
                PaymentId = form.PaymentId,
                RefundId = form.RefundId,
                OpenSessionRequestId = form.OpenSessionRequestId,
                AffectedStudentId = form.AffectedStudentId,
                ReasonCode = form.ReasonCode,
                Description = form.Description ?? "",
            };
            var files = Request.Form.Files.Count > 0 ? Request.Form.Files.ToList() : null;
            var detail = await _complaints.FileAsync(userId, role, request, files, cancellationToken);
            return NewResult(OkResponse(detail));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<ComplaintDetailDto>(ex.Message));
        }
    }

    [HttpPost(Router.StudentComplaintRespond)]
    public async Task<IActionResult> Respond(
        int id,
        [FromBody] RespondBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _complaints.RespondAsComplainantAsync(id, CurrentUserId(), body.Response, cancellationToken);
            return NewResult(OkResponse("Response submitted."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    private ComplaintComplainantRole ResolveRole() =>
        User.IsInRole("Guardian") ? ComplaintComplainantRole.Guardian : ComplaintComplainantRole.Student;

    private int CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
        if (!int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException();
        return id;
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

    public sealed class FileComplaintForm
    {
        public ComplaintSubjectType SubjectType { get; set; }
        public int? CourseScheduleId { get; set; }
        public int? EnrollmentId { get; set; }
        public int? PaymentId { get; set; }
        public int? RefundId { get; set; }
        public int? OpenSessionRequestId { get; set; }
        public int? AffectedStudentId { get; set; }
        public ComplaintReason ReasonCode { get; set; }
        public string? Description { get; set; }
    }

    public sealed class RespondBody
    {
        public string Response { get; set; } = "";
    }
}
