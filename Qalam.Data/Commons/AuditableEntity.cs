using System;

namespace Qalam.Data.Commons
{
    /// <summary>
    /// Base class for entities that require audit tracking
    /// </summary>
    public abstract class AuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

