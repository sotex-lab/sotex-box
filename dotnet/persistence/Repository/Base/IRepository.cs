using System.Linq.Expressions;
using DotNext;
using model.Core;

namespace persistence.Repository.Base;

public interface IRepository<TEntity, T>
    where TEntity : Entity<T>, new()
    where T : IComparable, IEquatable<T>
{
    Task<Result<TEntity, RepositoryError>> GetSingle(T id, CancellationToken token = default);
    Task<Result<TEntity, RepositoryError>> GetSingle(
        Expression<Func<TEntity, bool>> condition,
        CancellationToken token = default
    );
    IAsyncEnumerable<TEntity> Fetch(Expression<Func<TEntity, bool>>? condition = null);
    Task<Result<TEntity, RepositoryError>> Add(TEntity entity, CancellationToken token = default);
    Task<Result<TEntity, RepositoryError>> Update(
        TEntity entity,
        CancellationToken token = default
    );
    Task<Result<TEntity, RepositoryError>> Delete(
        TEntity entity,
        CancellationToken token = default
    );
}

public enum RepositoryError
{
    ArgumentNull,
    NotFound,
    FailedToInit,
    Duplicate,
    InvalidIpAddress,
    General
}

public static class RepositoryErrorExtensions
{
    public static string Stringify(this RepositoryError error) =>
        error switch
        {
            RepositoryError.ArgumentNull => "Passed argument was null\n",
            RepositoryError.NotFound => "Entity not found\n",
            RepositoryError.FailedToInit => "Failed to initialize database\n",
            RepositoryError.General => "Database persistence failed\n",
            RepositoryError.Duplicate => "Duplicate key found\n",
            RepositoryError.InvalidIpAddress => "Invalid ip address\n",
            RepositoryError => "Catch-all error, shouldn't happen\n"
        };
}
