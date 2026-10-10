using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LevelUp.Infrastructure.Persistence.Repositories;

public class Repository<TEntity>(LevelUpDbContext context) : IRepository<TEntity> where TEntity : AuditableEntity
{
    protected LevelUpDbContext Context { get; } = context;
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(e => e.Id == id, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    /// <summary>
    /// Tracked entities are change-detected automatically; only detached entities are attached,
    /// so related entities in a loaded graph are not needlessly marked as modified.
    /// </summary>
    public void Update(TEntity entity)
    {
        if (Context.Entry(entity).State == EntityState.Detached)
            Set.Update(entity);
    }

    public void Remove(TEntity entity) => Set.Remove(entity);
}
