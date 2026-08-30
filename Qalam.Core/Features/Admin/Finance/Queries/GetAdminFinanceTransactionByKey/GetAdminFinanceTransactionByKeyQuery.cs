using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminFinanceTransactionByKey;

public class GetAdminFinanceTransactionByKeyQuery : IRequest<Response<TeacherFinanceTransactionDetailDto>>
{
    public string Key { get; set; } = "";
}
