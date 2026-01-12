using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Curriculum management: List and manage curriculums
/// </summary>
[Authorize]
public class CurriculumController : AppControllerBase
{
    private readonly ICurriculumService _curriculumService;

    public CurriculumController(ICurriculumService curriculumService)
    {
        _curriculumService = curriculumService;
    }

    /// <summary>
    /// Get all curriculums with pagination
    /// </summary>
    [HttpGet(Router.Curriculum)]
    public async Task<IActionResult> GetCurriculums(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _curriculumService.GetPaginatedCurriculumsAsync(pageNumber, pageSize, search);
        return Ok(new Response<object>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Succeeded = true,
            Data = result
        });
    }

    /// <summary>
    /// Get curriculum by ID with its levels
    /// </summary>
    [HttpGet(Router.CurriculumById)]
    public async Task<IActionResult> GetCurriculumById(int id)
    {
        var curriculum = await _curriculumService.GetCurriculumWithLevelsAsync(id);
        if (curriculum == null)
            return NotFound(new Response<object>
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Succeeded = false,
                Message = "Curriculum not found"
            });

        return Ok(new Response<object>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Succeeded = true,
            Data = curriculum
        });
    }

    /// <summary>
    /// Get active curriculums only
    /// </summary>
    [HttpGet(Router.Curriculum + "/Active")]
    public async Task<IActionResult> GetActiveCurriculums()
    {
        var curriculums = await _curriculumService.GetActiveCurriculumsQueryable().ToListAsync();
        return Ok(new Response<object>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Succeeded = true,
            Data = curriculums
        });
    }
}
