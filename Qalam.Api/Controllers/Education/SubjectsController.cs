using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Subjects.Commands.CreateSubject;
using Qalam.Core.Features.Subjects.Commands.DeleteSubject;
using Qalam.Core.Features.Subjects.Commands.UpdateSubject;
using Qalam.Core.Features.Subjects.Queries.GetSubjectById;
using Qalam.Core.Features.Subjects.Queries.GetSubjectsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Subject management: CRUD operations for all subjects
/// </summary>
[Authorize]
public class SubjectsController : AppControllerBase
{
    /// <summary>
    /// Get all subjects with pagination and filters
    /// </summary>
    [HttpGet(Router.Subjects)]
    public async Task<IActionResult> GetSubjects([FromQuery] GetSubjectsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get subject by ID with full details
    /// </summary>
    [HttpGet(Router.SubjectById)]
    public async Task<IActionResult> GetSubjectById(int id)
    {
        return NewResult(await Mediator.Send(new GetSubjectByIdQuery { Id = id }));
    }

    /// <summary>
    /// Get subjects by grade ID
    /// </summary>
    [HttpGet(Router.SubjectsByGrade)]
    public async Task<IActionResult> GetSubjectsByGrade(int gradeId)
    {
        var query = new GetSubjectsListQuery { GradeId = gradeId, PageSize = 100 };
        return NewResult(await Mediator.Send(query));
    }

    // /// <summary>
    // /// Get subjects by domain ID
    // /// </summary>
    // [HttpGet(Router.SubjectsByDomain)]
    // public async Task<IActionResult> GetSubjectsByDomain(int domainId)
    // {
    //     var query = new GetSubjectsListQuery { DomainId = domainId, PageSize = 100 };
    //     return NewResult(await Mediator.Send(query));
    // }

    /// <summary>
    /// Create a new subject (Admin only)
    /// </summary>
    [HttpPost(Router.Subjects)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Update an existing subject (Admin only)
    /// </summary>
    [HttpPut(Router.SubjectById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
        return NewResult(await Mediator.Send(command));
    }

    /// <summary>
    /// Delete a subject (Admin only)
    /// </summary>
    [HttpDelete(Router.SubjectById)]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        return NewResult(await Mediator.Send(new DeleteSubjectCommand { Id = id }));
    }
}
