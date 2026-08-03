using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherStatusSummary;

public class GetTeacherStatusSummaryQuery : IRequest<Response<AdminTeacherStatusSummaryDto>>
{
}
