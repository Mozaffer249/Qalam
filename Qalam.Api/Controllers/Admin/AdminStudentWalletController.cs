using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.StudentWallets;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Wallet;

namespace Qalam.Api.Controllers.Admin;

/// <summary>Payer wallet of a student (guardian's wallet when the student has a guardian).</summary>
[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Student wallet")]
public class AdminStudentWalletController : AppControllerBase
{
    [HttpGet(Router.AdminStudentWallet)]
    [ProducesResponseType(typeof(AdminStudentWalletDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromRoute] int studentId, CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminStudentWalletQuery { StudentId = studentId }, cancellationToken));

    [HttpGet(Router.AdminStudentWalletTransactions)]
    [ProducesResponseType(typeof(List<WalletTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromRoute] int studentId,
        [FromQuery] string? type,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => NewResult(await Mediator.Send(new GetAdminStudentWalletTransactionsQuery
        {
            StudentId = studentId,
            Type = type,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken));

    /// <summary>Manual credit (positive amount) or debit (negative amount) with a required reason.</summary>
    [HttpPost(Router.AdminStudentWalletAdjustments)]
    [ProducesResponseType(typeof(AdminStudentWalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Adjust(
        [FromRoute] int studentId,
        [FromBody] AdminWalletAdjustmentDto body,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("uid")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _ = int.TryParse(userIdClaim, out var adminUserId);
        return NewResult(await Mediator.Send(new AdjustStudentWalletCommand
        {
            StudentId = studentId,
            AdminUserId = adminUserId,
            Data = body ?? new AdminWalletAdjustmentDto()
        }, cancellationToken));
    }
}
