using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Quran.Queries.GetQuranLevelsList;
using Qalam.Core.Features.Quran.Queries.GetQuranPartsList;
using Qalam.Core.Features.Quran.Queries.GetQuranSurahsList;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Education;

/// <summary>
/// Quran-related data: Levels, Parts, Surahs
/// </summary>
[Authorize]
public class QuranController : AppControllerBase
{
    /// <summary>
    /// Get all Quran levels with pagination
    /// </summary>
    [HttpGet(Router.QuranLevels)]
    public async Task<IActionResult> GetQuranLevels([FromQuery] GetQuranLevelsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    /// <summary>
    /// Get all Quran parts (Juz)
    /// </summary>
    [HttpGet(Router.QuranParts)]
    public async Task<IActionResult> GetQuranParts()
    {
        return NewResult(await Mediator.Send(new GetQuranPartsListQuery()));
    }

    /// <summary>
    /// Get all Quran surahs with pagination and filters
    /// </summary>
    [HttpGet(Router.QuranSurahs)]
    public async Task<IActionResult> GetQuranSurahs([FromQuery] GetQuranSurahsListQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }
}
