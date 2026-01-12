using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Commands.CreateContentUnit;

public class CreateContentUnitCommand : IRequest<Response<ContentUnit>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int OrderIndex { get; set; }
}
