using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface IAdRepository : IRepository<Ad, Guid>;

public class AdRepository(ApplicationDbContext context)
    : Repository<Ad, Guid, ApplicationDbContext>(context),
        IAdRepository;
