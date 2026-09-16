using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Teacher;

[ApiController]
[Authorize(Roles = "Teacher")]
[Tags("Teacher · Complaints")]
public class TeacherComplaintsController : AppControllerBase
{
    private readonly IComplaintService _complaints;
    private readonly ITeacherRepository _teachers;

    public TeacherComplaintsController(IComplaintService complaints, ITeacherRepository teachers)
    {
        _complaints = complaints;
        _teachers = teachers;
    }

    [HttpGet(Router.TeacherComplaints)]
    public async Task<IActionResult> List(
        [FromQuery] ComplaintStatus? status,
        [FromQuery] ComplaintSubjectType? subjectType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var teacher = await ResolveTeacherAsync(cancellationToken);
        var filter = new ComplaintListFilter
        {
            Status = status,
            SubjectType = subjectType,
            TeacherId = teacher.Id,
        };
        // Also include complaints filed by this teacher
        var (asRespondent, totalRespondent) = await _complaints.ListAsync(
            filter, pageNumber, pageSize, "Teacher", cancellationToken);
        var (asComplainant, totalComplainant) = await _complaints.ListAsync(
            new ComplaintListFilter
            {
                Status = status,
                SubjectType = subjectType,
                ComplainantUserId = teacher.UserId ?? CurrentUserId(),
            },
            pageNumber,
            pageSize,
            "Teacher",
            cancellationToken);

        var merged = asRespondent
            .Concat(asComplainant)
            .GroupBy(c => c.ComplaintId)
            .Select(g => g.First())
            .OrderByDescending(c => c.FiledAt)
            .Take(pageSize)
            .ToList();

        return NewResult(OkResponse(merged, new { total = totalRespondent + totalComplainant, pageNumber, pageSize }));
    }

    [HttpGet(Router.TeacherComplaintById)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var teacher = await ResolveTeacherAsync(cancellationToken);
        var detail = await _complaints.GetAsync(id, teacher.UserId ?? CurrentUserId(), "Teacher", teacher.Id, cancellationToken);
        if (detail == null)
            return NewResult(NotFoundResponse<ComplaintDetailDto>("Complaint not found."));
        return NewResult(OkResponse(detail));
    }

    [HttpPost(Router.TeacherComplaints)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> File(
        [FromForm] FileComplaintForm form,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var teacher = await ResolveTeacherAsync(cancellationToken);
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
            var userId = teacher.UserId ?? CurrentUserId();
            var detail = await _complaints.FileAsync(
                userId, ComplaintComplainantRole.Teacher, request, files, cancellationToken);
            return NewResult(OkResponse(detail));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<ComplaintDetailDto>(ex.Message));
        }
    }

    [HttpPost(Router.TeacherComplaintRespond)]
    public async Task<IActionResult> Respond(
        int id,
        [FromBody] RespondBody body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var teacher = await ResolveTeacherAsync(cancellationToken);
            await _complaints.RespondAsRespondentAsync(
                id, teacher.UserId ?? CurrentUserId(), teacher.Id, body.Response, cancellationToken);
            return NewResult(OkResponse("Response submitted."));
        }
        catch (InvalidOperationException ex)
        {
            return NewResult(BadRequestResponse<string>(ex.Message));
        }
    }

    private async Task<Data.Entity.Teacher.Teacher> ResolveTeacherAsync(CancellationToken cancellationToken)
    {
        var teacher = await _teachers.GetByUserIdAsync(CurrentUserId());
        if (teacher == null)
            throw new UnauthorizedAccessException();
        return teacher;
    }

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
