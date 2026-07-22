using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.AcademicPrograms.Commands.UpdateAcademicProgram;

public class UpdateAcademicProgramCommand : IRequest<Response<AcademicProgramDto>>
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? DegreeType { get; set; }
    public bool IsActive { get; set; } = true;
}
