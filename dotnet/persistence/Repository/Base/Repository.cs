using System.Linq.Expressions;
using DotNext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using model.Core;

namespace persistence.Repository.Base;

public abstract class Repository<TEntity, T, TContext> : IRepository<TEntity, T>
    where TEntity : Entity<T>, new()
    where T : IComparable, IEquatable<T>
    where TContext : DbContext
{
    protected TContext Context { get; }
    protected readonly DbSet<TEntity> DbSet;

    public Repository(TContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task<Result<TEntity, RepositoryError>> GetSingle(T id) =>
        await GetSingle(x => x.Id.Equals(id));

    public async Task<Result<TEntity, RepositoryError>> GetSingle(
        Expression<Func<TEntity, bool>> condition
    )
    {
        if (condition is null)
            return new Result<TEntity, RepositoryError>(RepositoryError.ArgumentNull);

        var instance = await DbSet.SingleOrDefaultAsync(condition);
        return instance != null
            ? new Result<TEntity, RepositoryError>(instance)
            : new Result<TEntity, RepositoryError>(RepositoryError.NotFound);
    }

    public IAsyncEnumerable<TEntity> Fetch(Expression<Func<TEntity, bool>>? condition = null)
    {
        return condition != null
            ? DbSet.Where(condition).AsAsyncEnumerable()
            : DbSet.AsAsyncEnumerable();
    }

    public async Task<Result<TEntity, RepositoryError>> Add(TEntity entity) =>
        await Execute(entity, DbSet.Add);

    public async Task<Result<TEntity, RepositoryError>> Update(TEntity entity) =>
        await Execute(entity, DbSet.Update);

    public async Task<Result<TEntity, RepositoryError>> Delete(TEntity entity) =>
        await Execute(entity, DbSet.Remove);

    private async Task<Result<TEntity, RepositoryError>> Execute(
        TEntity entity,
        Func<TEntity, EntityEntry<TEntity>> func
    )
    {
        if (entity is null)
            return new Result<TEntity, RepositoryError>(RepositoryError.ArgumentNull);
        try
        {
            func(entity);
            await Context.SaveChangesAsync();
        }
        catch (Exception)
        {
            return new Result<TEntity, RepositoryError>(RepositoryError.General);
        }
        return new Result<TEntity, RepositoryError>(entity);
    }
}
