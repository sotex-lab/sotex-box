using DotNext;
using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface IConfigurationRepository : IRepository<Configuration, string> { }

public class ConfigurationRepository(ApplicationDbContext context)
    : Repository<Configuration, string, ApplicationDbContext>(context),
        IConfigurationRepository { }
