using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Commands.UpsertWritableFilterValue;

public class UpsertWritableFilterValueCommand : IRequest<Response<WritableFilterValueDto>>
{
    public int DomainId { get; set; }
    public string SlotCode { get; set; } = string.Empty;

    /// <summary>Free-text path (teacher/admin quick add). Used as NameAr when NameAr is empty.</summary>
    public string? Text { get; set; }

    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Code { get; set; }
    public string? SubjectCodeContains { get; set; }
}
