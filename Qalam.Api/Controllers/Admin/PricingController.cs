using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.Pricing.Commands.ApproveLevelUpgradeSuggestion;
using Qalam.Core.Features.Admin.Pricing.Commands.BackfillStarterTeacherLevels;
using Qalam.Core.Features.Admin.Pricing.Commands.CreatePricingMarket;
using Qalam.Core.Features.Admin.Pricing.Commands.RejectLevelUpgradeSuggestion;
using Qalam.Core.Features.Admin.Pricing.Commands.SetDefaultPricingMarket;
using Qalam.Core.Features.Admin.Pricing.Commands.SetDomainSessionPrice;
using Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevel;
using Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherLevelTier;
using Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherShareOverride;
using Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingExchangeRate;
using Qalam.Core.Features.Admin.Pricing.Commands.UpdatePricingMarket;
using Qalam.Core.Features.Admin.Pricing.Queries.ListDomainSessionPrices;
using Qalam.Core.Features.Admin.Pricing.Queries.ListLevelUpgradeSuggestions;
using Qalam.Core.Features.Admin.Pricing.Queries.ListPricingExchangeRates;
using Qalam.Core.Features.Admin.Pricing.Queries.ListPricingMarkets;
using Qalam.Core.Features.Admin.Pricing.Queries.ListTeacherLevelTiers;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Route(Router.AdminPricing)]
[Authorize(Roles = Roles.SuperAdmin)]
[Tags("Admin · Pricing")]
public class PricingController : AppControllerBase
{
    [HttpGet("domain-session-prices")]
    public async Task<IActionResult> ListDomainSessionPrices(
        [FromQuery] string marketCode,
        [FromQuery] int? domainId,
        [FromQuery] string? sessionTypeCode,
        [FromQuery] bool includeHistory = false)
        => NewResult(await Mediator.Send(new ListDomainSessionPricesQuery
        {
            MarketCode = marketCode,
            DomainId = domainId,
            SessionTypeCode = sessionTypeCode,
            IncludeHistory = includeHistory
        }));

    [HttpGet("pricing-markets")]
    public async Task<IActionResult> ListPricingMarkets()
        => NewResult(await Mediator.Send(new ListAdminPricingMarketsQuery()));

    [HttpPost("pricing-markets")]
    public async Task<IActionResult> CreatePricingMarket([FromBody] CreatePricingMarketDto data)
        => NewResult(await Mediator.Send(new CreatePricingMarketCommand { Data = data }));

    [HttpPut("pricing-markets/{code}")]
    public async Task<IActionResult> UpdatePricingMarket(string code, [FromBody] UpdatePricingMarketDto data)
        => NewResult(await Mediator.Send(new UpdatePricingMarketCommand { Code = code, Data = data }));

    [HttpPut("pricing-markets/{code}/set-default")]
    public async Task<IActionResult> SetDefaultPricingMarket(string code)
        => NewResult(await Mediator.Send(new SetDefaultPricingMarketCommand { Code = code }));

    [HttpPut("domain-session-prices")]
    public async Task<IActionResult> SetDomainSessionPrice([FromBody] SetDomainSessionPriceDto data)
        => NewResult(await Mediator.Send(new SetDomainSessionPriceCommand { Data = data }));

    [HttpGet("exchange-rates")]
    public async Task<IActionResult> ListExchangeRates()
        => NewResult(await Mediator.Send(new ListPricingExchangeRatesQuery()));

    [HttpPut("exchange-rates/{code}")]
    public async Task<IActionResult> UpdateExchangeRate(string code, [FromBody] UpdatePricingExchangeRateDto data)
        => NewResult(await Mediator.Send(new UpdatePricingExchangeRateCommand { Code = code, Data = data }));

    [HttpGet("teacher-level-tiers")]
    public async Task<IActionResult> ListTeacherLevelTiers()
        => NewResult(await Mediator.Send(new ListTeacherLevelTiersQuery()));

    [HttpPut("teacher-level-tiers/{id:int}")]
    public async Task<IActionResult> SetTeacherLevelTier(int id, [FromBody] SetTeacherLevelTierDto data)
        => NewResult(await Mediator.Send(new SetTeacherLevelTierCommand { Id = id, Data = data }));

    [HttpPut("teachers/{teacherId:int}/level")]
    public async Task<IActionResult> SetTeacherLevel(int teacherId, [FromBody] SetTeacherLevelDto data)
        => NewResult(await Mediator.Send(new SetTeacherLevelCommand { TeacherId = teacherId, Data = data }));

    [HttpPut("teachers/{teacherId:int}/share-override")]
    public async Task<IActionResult> SetTeacherShareOverride(int teacherId, [FromBody] SetTeacherShareOverrideDto data)
        => NewResult(await Mediator.Send(new SetTeacherShareOverrideCommand { TeacherId = teacherId, Data = data }));

    [HttpGet("level-upgrade-suggestions")]
    public async Task<IActionResult> ListLevelUpgradeSuggestions([FromQuery] string status = "Pending")
        => NewResult(await Mediator.Send(new ListLevelUpgradeSuggestionsQuery { Status = status }));

    [HttpPost("level-upgrade-suggestions/{id:int}/approve")]
    public async Task<IActionResult> ApproveLevelUpgradeSuggestion(
        int id,
        [FromBody] ReviewLevelUpgradeSuggestionDto? data)
        => NewResult(await Mediator.Send(new ApproveLevelUpgradeSuggestionCommand { Id = id, Data = data }));

    [HttpPost("level-upgrade-suggestions/{id:int}/reject")]
    public async Task<IActionResult> RejectLevelUpgradeSuggestion(
        int id,
        [FromBody] ReviewLevelUpgradeSuggestionDto? data)
        => NewResult(await Mediator.Send(new RejectLevelUpgradeSuggestionCommand { Id = id, Data = data }));

    [HttpPost("teachers/backfill-starter-level")]
    public async Task<IActionResult> BackfillStarterTeacherLevels()
        => NewResult(await Mediator.Send(new BackfillStarterTeacherLevelsCommand()));
}
