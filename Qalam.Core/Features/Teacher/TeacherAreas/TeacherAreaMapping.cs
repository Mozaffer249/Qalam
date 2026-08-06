using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Core.Features.Teacher.TeacherAreas;

internal static class TeacherAreaMapping
{
    public static TeacherAreaResponseDto ToDto(TeacherArea area)
    {
        var dto = new TeacherAreaResponseDto
        {
            Id = area.Id,
            LocationId = area.LocationId,
            MaxDistanceKm = area.MaxDistanceKm,
            IsActive = area.IsActive,
            LocationNameAr = area.Location?.NameAr ?? string.Empty,
            LocationNameEn = area.Location?.NameEn ?? string.Empty,
            LocationType = area.Location?.Type ?? LocationType.Country
        };

        ApplyHierarchy(dto, area.Location);
        return dto;
    }

    private static void ApplyHierarchy(TeacherAreaResponseDto dto, Location? location)
    {
        for (var current = location; current != null; current = current.ParentLocation)
        {
            switch (current.Type)
            {
                case LocationType.District:
                    dto.DistrictNameAr ??= current.NameAr;
                    dto.DistrictNameEn ??= current.NameEn;
                    break;
                case LocationType.City:
                    dto.CityNameAr ??= current.NameAr;
                    dto.CityNameEn ??= current.NameEn;
                    break;
                case LocationType.Region:
                    dto.RegionNameAr ??= current.NameAr;
                    dto.RegionNameEn ??= current.NameEn;
                    break;
            }
        }
    }
}
