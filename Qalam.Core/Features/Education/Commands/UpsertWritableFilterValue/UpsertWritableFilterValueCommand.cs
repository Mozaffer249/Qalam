using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Commands.UpsertWritableFilterValue;

public class UpsertWritableFilterValueCommand : IRequest<Response<WritableFilterValueDto>>
{
    public int DomainId { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? NameEn { get; set; }
}
