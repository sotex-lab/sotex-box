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

    public async Task<Result<TEntity, RepositoryError>> GetSingle(
        T id,
        CancellationToken token = default
    ) => await GetSingle(x => x.Id.Equals(id), token);

    public async Task<Result<TEntity, RepositoryError>> GetSingle(
        Expression<Func<TEntity, bool>> condition,
        CancellationToken token = default
    )
    {
        if (condition is null)
            return new Result<TEntity, RepositoryError>(RepositoryError.ArgumentNull);

        var instance = await DbSet.SingleOrDefaultAsync(condition, token);
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

    public async Task<Result<TEntity, RepositoryError>> Add(
        TEntity entity,
        CancellationToken token = default
    ) => await Execute(entity, DbSet.Add, token);

    public async Task<Result<TEntity, RepositoryError>> Update(
        TEntity entity,
        CancellationToken token = default
    ) => await Execute(entity, DbSet.Update, token);

    public async Task<Result<TEntity, RepositoryError>> Delete(
        TEntity entity,
        CancellationToken token = default
    ) => await Execute(entity, DbSet.Remove, token);

    private async Task<Result<TEntity, RepositoryError>> Execute(
        TEntity entity,
        Func<TEntity, EntityEntry<TEntity>> func,
        CancellationToken token = default
    )
    {
        if (entity is null)
            return new Result<TEntity, RepositoryError>(RepositoryError.ArgumentNull);
        try
        {
            func(entity);
            await Context.SaveChangesAsync(token);
        }
        catch (Exception)
        {
            return new Result<TEntity, RepositoryError>(RepositoryError.General);
        }
        return new Result<TEntity, RepositoryError>(entity);
    }
}
