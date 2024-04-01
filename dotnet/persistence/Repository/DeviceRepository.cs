using DotNext;
using Microsoft.EntityFrameworkCore;
using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface IDeviceRepository : IRepository<Device, Guid>
{
    IEnumerable<Device> GetPage(uint page, uint pageSize);

    Task<int> Count();
}

public class DeviceRepository(ApplicationDbContext context)
    : Repository<Device, Guid, ApplicationDbContext>(context),
        IDeviceRepository
{
    public async Task<int> Count() => await DbSet.CountAsync();

    public IEnumerable<Device> GetPage(uint page, uint pageSize) =>
        DbSet.Skip((int)(page * pageSize)).Take((int)pageSize).AsEnumerable();
}
