using System.Linq.Expressions;
using DotNext;
using model.Core;

namespace persistence.Repository.Base;

public interface IRepository<TEntity, T>
    where TEntity : Entity<T>, new()
    where T : IComparable, IEquatable<T>
{
    Task<Result<TEntity, RepositoryError>> GetSingle(T id);
    Task<Result<TEntity, RepositoryError>> GetSingle(Expression<Func<TEntity, bool>> condition);
    IAsyncEnumerable<TEntity> Fetch(Expression<Func<TEntity, bool>>? condition = null);
    Task<Result<TEntity, RepositoryError>> Add(TEntity entity);
    Task<Result<TEntity, RepositoryError>> Update(TEntity entity);
    Task<Result<TEntity, RepositoryError>> Delete(TEntity entity);
}

public enum RepositoryError
{
    ArgumentNull,
    NotFound
}

public static class RepositoryErrorExtensions
{
    public static string Stringify(this RepositoryError error) =>
        error switch
        {
            RepositoryError.ArgumentNull => "Passed argument was null\n",
            RepositoryError.NotFound => "Entity not found\n",
            RepositoryError => "Catch-all error, shouldn't happen\n"
        };
}
