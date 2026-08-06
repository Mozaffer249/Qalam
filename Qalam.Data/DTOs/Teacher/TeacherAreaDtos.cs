using Qalam.Data.Entity.Common;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherAreaResponseDto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public decimal MaxDistanceKm { get; set; }
    public bool IsActive { get; set; }

    public string LocationNameAr { get; set; } = default!;
    public string LocationNameEn { get; set; } = default!;
    public LocationType LocationType { get; set; }

    public string? DistrictNameAr { get; set; }
    public string? DistrictNameEn { get; set; }
    public string? CityNameAr { get; set; }
    public string? CityNameEn { get; set; }
    public string? RegionNameAr { get; set; }
    public string? RegionNameEn { get; set; }
}

public class CreateTeacherAreaDto
{
    public int LocationId { get; set; }
    public decimal? MaxDistanceKm { get; set; }
}
