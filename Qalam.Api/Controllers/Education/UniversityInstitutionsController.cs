using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Education.AcademicPrograms.Commands.CreateAcademicProgram;
using Qalam.Core.Features.Education.AcademicPrograms.Commands.SetAcademicProgramActive;
using Qalam.Core.Features.Education.AcademicPrograms.Commands.UpdateAcademicProgram;
using Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramById;
using Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramsList;
using Qalam.Core.Features.Education.Colleges.Commands.CreateCollege;
using Qalam.Core.Features.Education.Colleges.Commands.SetCollegeActive;
using Qalam.Core.Features.Education.Colleges.Commands.UpdateCollege;
using Qalam.Core.Features.Education.Colleges.Queries.GetCollegeById;
using Qalam.Core.Features.Education.Colleges.Queries.GetCollegesList;
using Qalam.Core.Features.Education.Departments.Commands.CreateDepartment;
using Qalam.Core.Features.Education.Departments.Commands.SetDepartmentActive;
using Qalam.Core.Features.Education.Departments.Commands.UpdateDepartment;
using Qalam.Core.Features.Education.Departments.Queries.GetDepartmentById;
using Qalam.Core.Features.Education.Departments.Queries.GetDepartmentsList;
using Qalam.Core.Features.Education.Universities.Commands.CreateUniversity;
using Qalam.Core.Features.Education.Universities.Commands.SetUniversityActive;
using Qalam.Core.Features.Education.Universities.Commands.UpdateUniversity;
using Qalam.Core.Features.Education.Universities.Queries.GetUniversitiesList;
using Qalam.Core.Features.Education.Universities.Queries.GetUniversityById;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// University institution hierarchy: University → College → Department → AcademicProgram (CQRS via MediatR).
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public class UniversityInstitutionsController : AppControllerBase
{
    // ── Universities ──────────────────────────────────────────────

    [HttpGet(Router.EducationUniversities)]
    public async Task<IActionResult> GetUniversities([FromQuery] GetUniversitiesListQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet(Router.EducationUniversityById)]
    public async Task<IActionResult> GetUniversity(int id)
        => NewResult(await Mediator.Send(new GetUniversityByIdQuery { Id = id }));

    [HttpPost(Router.EducationUniversities)]
    public async Task<IActionResult> CreateUniversity([FromBody] CreateUniversityCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpPut(Router.EducationUniversityById)]
    public async Task<IActionResult> UpdateUniversity(int id, [FromBody] UpdateUniversityCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch between route and body");
        return NewResult(await Mediator.Send(command));
    }

    [HttpPatch(Router.EducationUniversities + "/{id}/toggle-status")]
    public async Task<IActionResult> ToggleUniversity(int id, [FromQuery] bool isActive = true)
        => NewResult(await Mediator.Send(new SetUniversityActiveCommand { Id = id, IsActive = isActive }));

    // ── Colleges ──────────────────────────────────────────────────

    [HttpGet(Router.EducationColleges)]
    public async Task<IActionResult> GetColleges([FromQuery] GetCollegesListQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet(Router.EducationCollegeById)]
    public async Task<IActionResult> GetCollege(int id)
        => NewResult(await Mediator.Send(new GetCollegeByIdQuery { Id = id }));

    [HttpPost(Router.EducationColleges)]
    public async Task<IActionResult> CreateCollege([FromBody] CreateCollegeCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpPut(Router.EducationCollegeById)]
    public async Task<IActionResult> UpdateCollege(int id, [FromBody] UpdateCollegeCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch between route and body");
        return NewResult(await Mediator.Send(command));
    }

    [HttpPatch(Router.EducationColleges + "/{id}/toggle-status")]
    public async Task<IActionResult> ToggleCollege(int id, [FromQuery] bool isActive = true)
        => NewResult(await Mediator.Send(new SetCollegeActiveCommand { Id = id, IsActive = isActive }));

    // ── Departments ───────────────────────────────────────────────

    [HttpGet(Router.EducationDepartments)]
    public async Task<IActionResult> GetDepartments([FromQuery] GetDepartmentsListQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet(Router.EducationDepartmentById)]
    public async Task<IActionResult> GetDepartment(int id)
        => NewResult(await Mediator.Send(new GetDepartmentByIdQuery { Id = id }));

    [HttpPost(Router.EducationDepartments)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpPut(Router.EducationDepartmentById)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch between route and body");
        return NewResult(await Mediator.Send(command));
    }

    [HttpPatch(Router.EducationDepartments + "/{id}/toggle-status")]
    public async Task<IActionResult> ToggleDepartment(int id, [FromQuery] bool isActive = true)
        => NewResult(await Mediator.Send(new SetDepartmentActiveCommand { Id = id, IsActive = isActive }));

    // ── Academic Programs ─────────────────────────────────────────

    [HttpGet(Router.EducationAcademicPrograms)]
    public async Task<IActionResult> GetPrograms([FromQuery] GetAcademicProgramsListQuery query)
        => NewResult(await Mediator.Send(query));

    [HttpGet(Router.EducationAcademicProgramById)]
    public async Task<IActionResult> GetProgram(int id)
        => NewResult(await Mediator.Send(new GetAcademicProgramByIdQuery { Id = id }));

    [HttpPost(Router.EducationAcademicPrograms)]
    public async Task<IActionResult> CreateProgram([FromBody] CreateAcademicProgramCommand command)
        => NewResult(await Mediator.Send(command));

    [HttpPut(Router.EducationAcademicProgramById)]
    public async Task<IActionResult> UpdateProgram(int id, [FromBody] UpdateAcademicProgramCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch between route and body");
        return NewResult(await Mediator.Send(command));
    }

    [HttpPatch(Router.EducationAcademicPrograms + "/{id}/toggle-status")]
    public async Task<IActionResult> ToggleProgram(int id, [FromQuery] bool isActive = true)
        => NewResult(await Mediator.Send(new SetAcademicProgramActiveCommand { Id = id, IsActive = isActive }));
}
