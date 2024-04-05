using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using model.Core;
using persistence.Repository.Base;

namespace persistence.Repository;

public interface IAdRepository : IRepository<Ad, Guid>
{
    Task<IEnumerable<Ad>> TakeFrom(Guid id, uint take);
}

public class AdRepository(ApplicationDbContext context)
    : Repository<Ad, Guid, ApplicationDbContext>(context),
        IAdRepository
{
    public async Task<IEnumerable<Ad>> TakeFrom(Guid id, uint take)
    {
        IQueryable<Ad> adsQuery = DbSet;

        if (id != Guid.Empty)
        {
            var ids = await DbSet.Select(x => x.Id).ToListAsync();
            var firstMatching = ids.Select((x, i) => (x, i)).Single(t => t.x == id);
            adsQuery = adsQuery.Skip(firstMatching.i);
        }

        var takenAds = await adsQuery.Take((int)take).ToListAsync();
        if (takenAds.Count < take && takenAds.Count < await DbSet.CountAsync())
        {
            takenAds.AddRange(DbSet.Take((int)take - takenAds.Count));
        }

        return takenAds;
    }
}
