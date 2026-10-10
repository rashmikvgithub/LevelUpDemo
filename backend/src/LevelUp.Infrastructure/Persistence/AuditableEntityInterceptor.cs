using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LevelUp.Infrastructure.Persistence;

/// <summary>Populates CreatedBy/CreatedDate/ModifiedBy/ModifiedDate on every save.</summary>
public sealed class AuditableEntityInterceptor(ICurrentUserService currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        var now = clock.UtcNow;
        var user = currentUser.UserName;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = user;
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedBy = user;
                    entry.Entity.ModifiedDate = now;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    entry.Property(e => e.CreatedDate).IsModified = false;
                    break;
            }
        }
    }
}
