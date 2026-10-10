using LevelUp.Domain.Common;

namespace LevelUp.Application.Common.Interfaces;

/// <summary>Generic repository for aggregate roots.</summary>
public interface IRepository<TEntity> where TEntity : AuditableEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

/// <summary>Commits all pending repository changes as a single transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Abstraction over the system clock so business logic is testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

/// <summary>Identifies the user performing the current operation (used for audit fields).</summary>
public interface ICurrentUserService
{
    string UserName { get; }
}
