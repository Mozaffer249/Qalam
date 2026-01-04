using System;

namespace Qalam.Data.Commons
{
    /// <summary>
    /// Base class for entities that support soft delete functionality
    /// </summary>
    public abstract class SoftDeletableEntity : AuditableEntity
    {
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}

