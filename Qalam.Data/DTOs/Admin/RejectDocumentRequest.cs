using System.ComponentModel.DataAnnotations;

namespace Qalam.Data.DTOs.Admin;

public class RejectDocumentRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;
}
