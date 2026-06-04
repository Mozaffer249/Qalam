using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.DeleteTeacherRegistrationRequirement;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.SetTeacherRegistrationRequirementActive;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.UpdateTeacherRegistrationRequirement;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.GetTeacherRegistrationRequirementById;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.ListTeacherRegistrationRequirements;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// SuperAdmin CRUD for the teacher registration requirements catalog (files, text, boolean fields).
/// </summary>
/// <remarks>
/// Controls which fields teachers must complete during registration. Teacher apps read **active** items via
/// `GET /Api/V1/Authentication/Teacher/RegistrationRequirements`.
///
/// See `docs/Teacher-Registration-Requirements.md`.
/// </remarks>
[ApiController]
[Route(Router.AdminTeacherRegistrationRequirements)]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · Teacher registration requirements")]
public class TeacherRegistrationRequirementsController : AppControllerBase
{
    /// <summary>
    /// List all registration requirements (including inactive).
    /// </summary>
    /// <returns>Full admin catalog ordered by sort order</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeacherRegistrationRequirementAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        return NewResult(await Mediator.Send(new ListTeacherRegistrationRequirementsQuery()));
    }

    /// <summary>
    /// Get a single registration requirement by ID.
    /// </summary>
    /// <param name="id">Requirement ID</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeacherRegistrationRequirementAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return NewResult(await Mediator.Send(new GetTeacherRegistrationRequirementByIdQuery { Id = id }));
    }

    /// <summary>
    /// Create a custom registration requirement.
    /// </summary>
    /// <param name="data">Code, type (File/Text/Boolean), labels, validation limits</param>
    /// <remarks>
    /// `code` must be unique and stable (used by teacher submit as `file_{code}` for File types).
    /// System-seeded codes (`identity_document`, `certificate`, `bio`, `location`) are created by the seeder.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherRegistrationRequirementAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTeacherRegistrationRequirementDto data)
    {
        return NewResult(await Mediator.Send(new CreateTeacherRegistrationRequirementCommand { Data = data }));
    }

    /// <summary>
    /// Update an existing registration requirement.
    /// </summary>
    /// <param name="id">Requirement ID</param>
    /// <param name="data">Labels, active/required flags, sort order, validation limits</param>
    /// <remarks>`code` cannot be changed after creation.</remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeacherRegistrationRequirementAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTeacherRegistrationRequirementDto data)
    {
        return NewResult(await Mediator.Send(new UpdateTeacherRegistrationRequirementCommand { Id = id, Data = data }));
    }

    /// <summary>
    /// Delete a registration requirement.
    /// </summary>
    /// <param name="id">Requirement ID</param>
    /// <remarks>
    /// Fails for system requirements (`isSystem`) or when submissions exist.
    /// Prefer `PATCH …/{id}/active` to disable without deleting.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        return NewResult(await Mediator.Send(new DeleteTeacherRegistrationRequirementCommand { Id = id }));
    }

    /// <summary>
    /// Toggle whether a requirement is active (shown to teachers during registration).
    /// </summary>
    /// <param name="id">Requirement ID</param>
    /// <param name="data">`isActive` flag</param>
    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(typeof(TeacherRegistrationRequirementAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetRequirementActiveDto data)
    {
        return NewResult(await Mediator.Send(new SetTeacherRegistrationRequirementActiveCommand { Id = id, Data = data }));
    }
}
