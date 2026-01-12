using MediatR;
using Microsoft.EntityFrameworkCore;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Quran;

namespace Qalam.Core.Features.Quran.Queries.GetQuranPartsList;

public class GetQuranPartsListQuery : IRequest<Response<List<QuranPart>>>
{
}
