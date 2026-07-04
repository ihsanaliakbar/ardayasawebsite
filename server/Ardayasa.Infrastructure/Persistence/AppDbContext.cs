using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Psychologist> Psychologists => Set<Psychologist>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
            e.Property(t => t.TokenHash).HasMaxLength(128);
            e.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
            e.Property(t => t.CreatedByIp).HasMaxLength(64);
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Psychologist>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.DisplayName).HasMaxLength(200);
            e.Property(p => p.Title).HasMaxLength(200);
            e.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Psychologist>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.TimestampUtc);
            e.Property(a => a.Action).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(64);
        });
    }
}
