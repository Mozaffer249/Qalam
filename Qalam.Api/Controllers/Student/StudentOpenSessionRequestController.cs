using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CancelOpenSessionRequest;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.CreateOpenSessionRequest;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.DeleteOpenSessionRequestAttachment;
using Qalam.Core.Features.Student.OpenSessionRequests.Commands.UploadOpenSessionRequestAttachment;
using Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetMyOpenSessionRequests;
using Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetOpenSessionRequestById;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Open Session Request — student (or guardian on behalf of a minor) posts a request
/// for sessions and receives offers from qualified teachers.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
[Route("Api/V1/Student")]
public class StudentOpenSessionRequestController : AppControllerBase
{
    /// <summary>
    /// Create + publish a new open session request. By default the broadcast matching engine
    /// runs when no invitations are pending; supplying <c>targetedTeacherId</c> sends the request
    /// to a single teacher instead. Otherwise the request waits in PendingInvitations until all
    /// invited students respond.
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/OpenSessionRequests
    ///
    /// Authorization:
    /// - Adult student: `data.studentId` must be the caller's linked Student.Id.
    /// - Guardian on behalf of minor: `data.studentId` is the child Student.Id; guardian must
    ///   be the linked Guardian. The server stores `createdByGuardianId` for audit.
    ///
    /// Status transitions on create:
    /// - No invitations → `Active` (dispatch fires immediately — see "Dispatch" below).
    /// - With invitations → `PendingInvitations` (dispatch waits until all invitees respond).
    ///
    /// Dispatch on Active (S2-ST-001 vs S2-ST-001b):
    /// - `targetedTeacherId` omitted/null → broadcast matching runs; every qualified teacher
    ///   gets an `OpenSessionRequestTarget` row + notification email.
    /// - `targetedTeacherId` set → broadcast is **skipped**. The server first validates that the
    ///   teacher exists/active AND offers `data.subjectId` via an active `TeacherSubject` row,
    ///   then hard-validates each session's `units[]` against that teacher's `TeacherSubjectUnits`
    ///   (anything outside → 400). Only that one teacher gets a Target row + notification.
    ///
    /// `units[]` per session — each row must set EXACTLY ONE of `contentUnitId` / `lessonId`:
    /// - `{ contentUnitId, includesAllLessons: true }` → cover every lesson in the unit.
    /// - `{ contentUnitId, includesAllLessons: false }` (or flag omitted) → unit as topic header only.
    /// - `{ lessonId }` → only that lesson. `includesAllLessons` must be `false`/omitted —
    ///   single-lesson rows can't expand (400 otherwise).
    ///
    /// `expiresAt` defaults to PublishedAt + 7 days if omitted.
    ///
    /// ─────────────────────────────────────────────
    /// Request body samples
    /// ─────────────────────────────────────────────
    ///
    /// **Case A — Broadcast (default), individual session, no content units.**
    /// Matching runs at publish. Every qualified teacher gets a Target row + email.
    /// ```json
    /// {
    ///   "data": {
    ///     "studentId": 5,
    ///     "domainId": 1,
    ///     "subjectId": 12,
    ///     "teachingModeId": 1,
    ///     "totalSessionsCount": 2,
    ///     "studentNotes": "Prefers evenings.",
    ///     "sessions": [
    ///       { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60, "units": [] },
    ///       { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60, "units": [] }
    ///     ]
    ///   }
    /// }
    /// ```
    ///
    /// **Case B — Targeted teacher with all three `units[]` shapes.**
    /// Broadcast is skipped. Server validates the teacher offers `subjectId` AND each row's
    /// `contentUnitId` / `lessonId` is in that teacher's TeacherSubjectUnits.
    /// ```json
    /// {
    ///   "data": {
    ///     "studentId": 5,
    ///     "domainId": 1,
    ///     "subjectId": 12,
    ///     "teachingModeId": 1,
    ///     "targetedTeacherId": 42,
    ///     "totalSessionsCount": 3,
    ///     "sessions": [
    ///       { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60,
    ///         "units": [ { "contentUnitId": 115, "includesAllLessons": true } ] },
    ///       { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60,
    ///         "units": [ { "contentUnitId": 116, "includesAllLessons": false } ] },
    ///       { "sequenceNumber": 3, "preferredDate": "2026-06-14", "timeSlotId": 3, "durationMinutes": 60,
    ///         "units": [ { "lessonId": 4501 } ] }
    ///     ]
    ///   }
    /// }
    /// ```
    ///
    /// **Case C — Group with invitations.** Status lands in `PendingInvitations`; dispatch
    /// (broadcast OR targeted) waits until every invitee responds.
    /// ```json
    /// {
    ///   "data": {
    ///     "studentId": 5,
    ///     "domainId": 1,
    ///     "subjectId": 12,
    ///     "teachingModeId": 2,
    ///     "groupType": "InviteOnly",
    ///     "totalSessionsCount": 2,
    ///     "invitedStudentIds": [ 19, 27 ],
    ///     "sessions": [
    ///       { "sequenceNumber": 1, "preferredDate": "2026-06-15", "timeSlotId": 4, "durationMinutes": 90, "units": [] },
    ///       { "sequenceNumber": 2, "preferredDate": "2026-06-17", "timeSlotId": 4, "durationMinutes": 90, "units": [] }
    ///     ]
    ///   }
    /// }
    /// ```
    ///
    /// **Case D — Quran domain.** When the domain code is `quran`, every session row MUST
    /// include `quranContentTypeId` (1=حفظ / 2=تلاوة / 3=تجويد) AND `quranLevelId`
    /// (1=نوراني / 2=مبتدئ / 3=متوسط / 4=متقدم). Targeted-teacher path also works here.
    /// ```json
    /// {
    ///   "data": {
    ///     "studentId": 5,
    ///     "domainId": 2,
    ///     "subjectId": 499,
    ///     "teachingModeId": 1,
    ///     "targetedTeacherId": 7,
    ///     "totalSessionsCount": 2,
    ///     "sessions": [
    ///       { "sequenceNumber": 1, "preferredDate": "2026-06-10", "timeSlotId": 3, "durationMinutes": 60,
    ///         "quranContentTypeId": 1, "quranLevelId": 2,
    ///         "units": [ { "contentUnitId": 200, "includesAllLessons": true } ] },
    ///       { "sequenceNumber": 2, "preferredDate": "2026-06-12", "timeSlotId": 3, "durationMinutes": 60,
    ///         "quranContentTypeId": 2, "quranLevelId": 2,
    ///         "units": [ { "lessonId": 12345 } ] }
    ///     ]
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpPost(Router.StudentOpenSessionRequests)]
    [ProducesResponseType(typeof(OpenSessionRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateOpenSessionRequestCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Get full detail of one open session request. Visible to the request owner (or their
    /// guardian) and to any student who was invited as a co-learner.
    /// </summary>
    [HttpGet(Router.StudentOpenSessionRequestById)]
    [ProducesResponseType(typeof(OpenSessionRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return NewResult(await Mediator.Send(new GetOpenSessionRequestByIdQuery { Id = id }));
    }

    /// <summary>
    /// List the caller's open session requests (created by self OR by self as a guardian on
    /// behalf of a child). Optional status filter.
    /// </summary>
    [HttpGet(Router.StudentOpenSessionRequestsMy)]
    [ProducesResponseType(typeof(List<OpenSessionRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy([FromQuery] OpenSessionRequestStatus? status)
    {
        return NewResult(await Mediator.Send(new GetMyOpenSessionRequestsQuery { Status = status }));
    }

    /// <summary>
    /// Cancel a request. Allowed while status is in
    /// { Draft, PendingInvitations, Active, ReceivingOffers }. Any Pending offers are bulk-
    /// withdrawn so teachers stop seeing the request as actionable.
    /// </summary>
    [HttpPost(Router.StudentOpenSessionRequestCancel)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOpenSessionRequestCommand command)
    {
        command.Id = id;
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Upload a single attachment file to an open session request (PDF, DOC, DOCX, PNG, JPG; max 25 MB; max 10 per request).
    /// The file is queued to OSS via RabbitMQ; the response carries the new attachment row with a placeholder
    /// <c>storageKey</c>. The <c>publicUrl</c> field is populated by the consumer shortly after upload completes.
    /// </summary>
    /// <remarks>
    /// POST Api/V1/Student/OpenSessionRequests/{id}/Attachments
    /// Content-Type: multipart/form-data
    /// Body: <c>file</c> (single IFormFile)
    /// </remarks>
    [HttpPost(Router.StudentOpenSessionRequestAttachments)]
    [RequestSizeLimit(26 * 1024 * 1024)] // 25 MB + headroom
    [ProducesResponseType(typeof(OpenSessionRequestAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAttachment(int id, [FromForm] IFormFile file)
    {
        var command = new UploadOpenSessionRequestAttachmentCommand
        {
            OpenSessionRequestId = id,
            File = file,
        };
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Remove an attachment from an open session request. Allowed while the request is still editable
    /// (Draft / PendingInvitations / Active / ReceivingOffers).
    /// </summary>
    [HttpDelete(Router.StudentOpenSessionRequestAttachmentById)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
    {
        var command = new DeleteOpenSessionRequestAttachmentCommand
        {
            OpenSessionRequestId = id,
            AttachmentId = attachmentId,
        };
        return NewResult(await Mediator.Send(command));
    }
}
