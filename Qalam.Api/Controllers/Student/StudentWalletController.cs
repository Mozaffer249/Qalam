using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Student.Wallet.Commands.CreateWalletTopUp;
using Qalam.Core.Features.Student.Wallet.Commands.MockWalletTopUp;
using Qalam.Core.Features.Student.Wallet.Queries.GetMyWallet;
using Qalam.Core.Features.Student.Wallet.Queries.GetMyWalletTransactions;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Wallet;

namespace Qalam.Api.Controllers.Student;

/// <summary>
/// Prepaid wallet of the signed-in payer (student or guardian): balance, ledger, top-ups.
/// Paying from the wallet lives on <see cref="StudentPaymentController"/>.
/// </summary>
[Authorize(Roles = Roles.Student + "," + Roles.Guardian)]
[ApiController]
public class StudentWalletController : AppControllerBase
{
    [HttpGet(Router.StudentWallet)]
    [ProducesResponseType(typeof(WalletSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWallet()
        => NewResult(await Mediator.Send(new GetMyWalletQuery()));

    [HttpGet(Router.StudentWalletTransactions)]
    [ProducesResponseType(typeof(List<WalletTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTransactions([FromQuery] GetMyWalletTransactionsQuery query)
        => NewResult(await Mediator.Send(query));

    /// <summary>Start a top-up checkout (same payload as enrollment payment intents).</summary>
    [HttpPost(Router.StudentWalletTopUps)]
    [ProducesResponseType(typeof(PaymentIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTopUp([FromBody] CreateWalletTopUpCommand command)
        => NewResult(await Mediator.Send(command));

    /// <summary>Instant top-up when the Mock gateway is active (dev / staging).</summary>
    [HttpPost(Router.StudentWalletMockTopUp)]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MockTopUp([FromBody] MockWalletTopUpCommand command)
        => NewResult(await Mediator.Send(command));
}
