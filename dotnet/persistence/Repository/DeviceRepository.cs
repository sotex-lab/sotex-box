using DotNext;
using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface IDeviceRepository : IRepository<Device, Guid> { }

public class DeviceRepository(ApplicationDbContext context)
    : Repository<Device, Guid, ApplicationDbContext>(context),
        IDeviceRepository { }
