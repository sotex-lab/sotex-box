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

        if (id == Guid.Empty)
            adsQuery = adsQuery.SkipWhile(x => x.Id != id);

        var takenAds = await adsQuery.Take((int)take).ToListAsync();
        if (takenAds.Count < take && takenAds.Count < await DbSet.CountAsync())
        {
            takenAds.AddRange(DbSet.Take((int)take - takenAds.Count));
        }

        return takenAds;
    }
}
