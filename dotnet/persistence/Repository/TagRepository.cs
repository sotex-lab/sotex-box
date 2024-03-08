using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface ITagRepository : IRepository<Tag, Guid>;

public class TagRepository(ApplicationDbContext context)
    : Repository<Tag, Guid, ApplicationDbContext>(context),
        ITagRepository;
