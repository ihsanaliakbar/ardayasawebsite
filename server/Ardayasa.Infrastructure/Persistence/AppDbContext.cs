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

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();

    public DbSet<Article> Articles => Set<Article>();

    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

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
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.DisplayName).HasMaxLength(200);
            e.Property(p => p.Title).HasMaxLength(200);
            e.Property(p => p.Slug).HasMaxLength(200);
            e.Property(p => p.Specialization).HasMaxLength(200);
            e.Property(p => p.Bio).HasMaxLength(4000);
            e.Property(p => p.PhotoKey).HasMaxLength(300);
            e.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Psychologist>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceCategory>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(1000);
        });

        builder.Entity<Service>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.Description).HasMaxLength(1000);
            e.Property(s => s.Notes).HasMaxLength(300);
            e.Property(s => s.OfflinePrice).HasPrecision(12, 0);
            e.Property(s => s.OnlinePrice).HasPrecision(12, 0);
            e.HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ArticleCategory>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Slug).HasMaxLength(200);
        });

        builder.Entity<Article>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();
            e.HasIndex(a => new { a.Status, a.PublishedAtUtc });
            e.Property(a => a.Title).HasMaxLength(300);
            e.Property(a => a.Slug).HasMaxLength(300);
            e.Property(a => a.Excerpt).HasMaxLength(1000);
            e.Property(a => a.FeaturedImageKey).HasMaxLength(300);
            e.HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FaqItem>(e =>
        {
            e.Property(f => f.Question).HasMaxLength(500);
            e.Property(f => f.AnswerHtml).HasMaxLength(4000);
        });

        builder.Entity<Testimonial>(e =>
        {
            e.Property(t => t.AuthorName).HasMaxLength(200);
            e.Property(t => t.RoleLabel).HasMaxLength(200);
            e.Property(t => t.Content).HasMaxLength(2000);
            e.HasOne(t => t.Psychologist)
                .WithMany()
                .HasForeignKey(t => t.PsychologistId)
                .OnDelete(DeleteBehavior.SetNull);
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
