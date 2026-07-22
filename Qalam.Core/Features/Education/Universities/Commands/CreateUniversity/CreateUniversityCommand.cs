using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Universities.Commands.CreateUniversity;

public class CreateUniversityCommand : IRequest<Response<UniversityDto>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
}
