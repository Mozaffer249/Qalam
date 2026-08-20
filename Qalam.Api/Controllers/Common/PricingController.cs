using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Common.Pricing.Commands.SetMyPricingMarket;
using Qalam.Core.Features.Common.Pricing.Queries.GetMyPricingMarket;
using Qalam.Core.Features.Common.Pricing.Queries.ListPricingMarkets;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Api.Controllers.Common;

[ApiController]
[Route(Router.CommonPricing)]
[Authorize]
[Tags("Common · Pricing")]
public class PricingController : AppControllerBase
{
    [HttpGet("markets")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PricingMarketDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMarkets()
        => NewResult(await Mediator.Send(new ListPricingMarketsQuery()));

    [HttpGet("my-market")]
    [ProducesResponseType(typeof(MyPricingMarketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMarket()
        => NewResult(await Mediator.Send(new GetMyPricingMarketQuery()));

    [HttpPut("my-market")]
    [ProducesResponseType(typeof(MyPricingMarketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMyMarket([FromBody] SetMyPricingMarketDto data)
        => NewResult(await Mediator.Send(new SetMyPricingMarketCommand { Data = data }));
}
