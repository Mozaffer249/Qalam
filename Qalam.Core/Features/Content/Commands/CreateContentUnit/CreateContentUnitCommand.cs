using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Commands.CreateContentUnit;

public class CreateContentUnitCommand : IRequest<Response<ContentUnit>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int? TermId { get; set; }  // Optional, null for Quran units
    public int OrderIndex { get; set; }
    public string UnitTypeCode { get; set; } = "SchoolUnit";  // SchoolUnit, QuranSurah, QuranPart, LanguageModule
    public int? QuranSurahId { get; set; }  // Optional, for Quran Surah units
    public int? QuranPartId { get; set; }  // Optional, for Quran Part units
}
