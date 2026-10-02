using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Identity;

namespace Qalam.Data.Entity.Payment;

/// <summary>
/// Prepaid balance owned by the paying account (student or guardian user).
/// <see cref="Balance"/> always equals the sum of <see cref="Transactions"/> amounts.
/// Writes are serialized per user with <c>sp_getapplock</c> (no row-version token).
/// </summary>
public class StudentWallet : AuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public decimal Balance { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
