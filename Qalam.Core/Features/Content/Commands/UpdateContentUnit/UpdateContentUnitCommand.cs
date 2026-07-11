using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Commands.UpdateContentUnit;

public class UpdateContentUnitCommand : IRequest<Response<ContentUnit>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int? TermId { get; set; }
    public int OrderIndex { get; set; }
    public string UnitTypeCode { get; set; } = "SchoolUnit";
    public int? QuranSurahId { get; set; }
    public int? QuranPartId { get; set; }
    public bool IsActive { get; set; } = true;
}
