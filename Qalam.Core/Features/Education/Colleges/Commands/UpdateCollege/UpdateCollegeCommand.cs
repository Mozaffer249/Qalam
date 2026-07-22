using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Colleges.Commands.UpdateCollege;

public class UpdateCollegeCommand : IRequest<Response<CollegeDto>>
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}
