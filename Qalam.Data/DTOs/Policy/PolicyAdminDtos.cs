namespace Qalam.Data.DTOs.Policy;

public class PolicyVersionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    /// <summary>Draft, Published or Retired.</summary>
    public string Status { get; set; } = "";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>True when this is the version in force right now.</summary>
    public bool IsCurrent { get; set; }
    public string? ChangeNote { get; set; }
    public int? PublishedByUserId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public CancellationPolicyRules Rules { get; set; } = new();
}

public class SavePolicyDraftDto
{
    public CancellationPolicyRules Rules { get; set; } = new();
    public string? ChangeNote { get; set; }
}

public class PublishPolicyDraftDto
{
    /// <summary>UTC; null = now. Must not be in the past.</summary>
    public DateTime? EffectiveFrom { get; set; }
}

public class PolicyDiffEntryDto
{
    /// <summary>Dotted path such as <c>sessionCancellation.noticeHours</c>.</summary>
    public string Path { get; set; } = "";
    public string? From { get; set; }
    public string? To { get; set; }
}

public class PolicyCompareDto
{
    public int FromVersionId { get; set; }
    public int ToVersionId { get; set; }
    public List<PolicyDiffEntryDto> Changes { get; set; } = new();
}
