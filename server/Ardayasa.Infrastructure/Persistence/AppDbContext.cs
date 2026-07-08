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

    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();

    public DbSet<PatientAssignment> PatientAssignments => Set<PatientAssignment>();

    public DbSet<LogbookEntry> LogbookEntries => Set<LogbookEntry>();

    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();

    public DbSet<AvailabilityException> AvailabilityExceptions => Set<AvailabilityException>();

    public DbSet<PsychologistService> PsychologistServices => Set<PsychologistService>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<ClinicSetting> ClinicSettings => Set<ClinicSetting>();

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

        builder.Entity<PatientProfile>(e =>
        {
            e.HasKey(p => p.UserId);
            e.Property(p => p.FullName).HasMaxLength(200);
            e.Property(p => p.BirthPlace).HasMaxLength(200);
            e.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.DomicileAddress).HasMaxLength(1000);
            e.Property(p => p.MaritalStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.LastEducation).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Occupation).HasMaxLength(200);
            e.Property(p => p.PriorDiagnosis).HasMaxLength(2000);
            e.Property(p => p.ConsultationConcerns).HasMaxLength(4000);
            e.Property(p => p.CounselingExpectations).HasMaxLength(4000);
            e.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<PatientProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PatientAssignment>(e =>
        {
            // The unique index is the DB-level guarantee against duplicate assignments.
            e.HasIndex(a => new { a.PatientUserId, a.PsychologistId }).IsUnique();
            e.HasIndex(a => a.PsychologistId);
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.PatientUserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Psychologist)
                .WithMany()
                .HasForeignKey(a => a.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LogbookEntry>(e =>
        {
            e.HasIndex(l => l.PatientUserId);
            e.Property(l => l.CaseSummary).HasMaxLength(4000);
            e.Property(l => l.SessionActivities).HasMaxLength(4000);
            e.Property(l => l.Homework).HasMaxLength(4000);
            e.Property(l => l.NextSessionPlan).HasMaxLength(4000);
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(l => l.PatientUserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Entries must survive the author's unassignment and outlive staff churn:
            // Restrict, so a psychologist with logbook history can't be hard-deleted.
            e.HasOne(l => l.AuthorPsychologist)
                .WithMany()
                .HasForeignKey(l => l.AuthorPsychologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AvailabilityRule>(e =>
        {
            e.HasIndex(r => r.PsychologistId);
            e.Property(r => r.DayOfWeek).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Psychologist)
                .WithMany()
                .HasForeignKey(r => r.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AvailabilityException>(e =>
        {
            e.HasIndex(x => new { x.PsychologistId, x.Date });
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Psychologist)
                .WithMany()
                .HasForeignKey(x => x.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PsychologistService>(e =>
        {
            // The unique index is the DB-level guarantee against duplicate mappings.
            e.HasIndex(m => new { m.PsychologistId, m.ServiceId }).IsUnique();
            e.HasOne(m => m.Psychologist)
                .WithMany()
                .HasForeignKey(m => m.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Service)
                .WithMany()
                .HasForeignKey(m => m.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Booking>(e =>
        {
            e.Property(b => b.Mode).HasConversion<string>().HasMaxLength(20);
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(b => b.PriceIdr).HasPrecision(12, 0);
            e.Property(b => b.ZoomLink).HasMaxLength(500);
            e.HasIndex(b => b.PatientUserId);
            e.HasIndex(b => new { b.PsychologistId, b.StartUtc }, "IX_Bookings_Psychologist_Start");
            // DB-level double-booking guard: only one active booking may hold a
            // given slot start. The quoted-identifier filter works on both Npgsql
            // (production) and SQLite (tests). A Postgres exclusion constraint in
            // the migration additionally rejects overlapping ranges.
            e.HasIndex(b => new { b.PsychologistId, b.StartUtc }, "IX_Bookings_ActiveSlot")
                .IsUnique()
                .HasFilter("\"Status\" IN ('PendingPayment', 'AwaitingVerification', 'Confirmed')");
            // Bookings are history: they must never vanish via cascade. The Phase 5
            // account-deletion path anonymizes them explicitly.
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(b => b.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Psychologist)
                .WithMany()
                .HasForeignKey(b => b.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClinicSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(1000);
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
