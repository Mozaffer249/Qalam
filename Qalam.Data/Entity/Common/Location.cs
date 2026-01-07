using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Common;

public class Location : AuditableEntity
{
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string NameAr { get; set; } = default!;
    
    [Required, MaxLength(100)]
    public string NameEn { get; set; } = default!;
    
    public int? ParentLocationId { get; set; }
    public Location? ParentLocation { get; set; }
    
    public LocationType Type { get; set; } // Country/Region/City/District
    
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public ICollection<Location> ChildLocations { get; set; } = new List<Location>();
}

public enum LocationType
{
    Country = 1,
    Region = 2,
    City = 3,
    District = 4
}

