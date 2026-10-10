using LevelUp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LevelUp.Infrastructure.Persistence.Configurations;

internal static class AuditConfiguration
{
    public static void ConfigureAudit<T>(this EntityTypeBuilder<T> builder) where T : AuditableEntity
    {
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CreatedDate).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);
        builder.Property(e => e.ModifiedDate).HasColumnType("datetime2");
    }
}
