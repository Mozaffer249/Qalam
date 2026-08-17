using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Commands.UpdateWritableFilterValue;

public class UpdateWritableFilterValueCommand : IRequest<Response<WritableFilterValueDto>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? SubjectCodeContains { get; set; }
    public bool IsActive { get; set; } = true;
}
