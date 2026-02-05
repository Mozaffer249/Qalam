using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IDayOfWeekRepository : IGenericRepositoryAsync<DayOfWeekMaster>
{
    IQueryable<DayOfWeekMaster> GetDaysOfWeekQueryable();
    IQueryable<DayOfWeekMaster> GetActiveDaysOfWeekQueryable();
}
