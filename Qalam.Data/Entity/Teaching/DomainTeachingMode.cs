using Qalam.Data.Entity.Education;

namespace Qalam.Data.Entity.Teaching;

public class DomainTeachingMode
{
    public int Id { get; set; }
    
    public int DomainId { get; set; }
    public EducationDomain Domain { get; set; } = default!;
    
    public int TeachingModeId { get; set; }
    public TeachingMode TeachingMode { get; set; } = default!;
    
    public bool IsAllowed { get; set; } = true;
}

