using DotNext;
using DotNext.Collections.Generic;
using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface ITagRepository : IRepository<Tag, Guid>
{
    IAsyncEnumerable<Tag> FindByNames(IEnumerable<string> names);
};

public class TagRepository(ApplicationDbContext context)
    : Repository<Tag, Guid, ApplicationDbContext>(context),
        ITagRepository
{
    public IAsyncEnumerable<Tag> FindByNames(IEnumerable<string> names)
    {
        var mappedNames = names.Select(x => x.Trim().ToLower()).ToList();
        return Fetch(x => mappedNames.Contains(x.Name));
    }
};
