using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data;

/// <summary>
/// EF Core code-first context. The configuration below mirrors the Part 1 SQL script
/// (VARCHAR columns, DECIMAL precisions, defaults, unique constraints, no cascade deletes).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleID);
            e.Property(x => x.RoleName).HasMaxLength(20).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();

            // Both roles always exist
            e.HasData(
                new Role { RoleID = 1, RoleName = RoleNames.Organiser },
                new Role { RoleID = 2, RoleName = RoleNames.Participant });
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleID)
             .HasConstraintName("FK_Users_Roles")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsUnicode(false).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            e.HasOne(x => x.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(x => x.OrganiserID)
             .HasConstraintName("FK_Events_Users")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(5, 2);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);
            e.Property(x => x.EntryFee).HasPrecision(8, 2);

            e.HasOne(x => x.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(x => x.EventID)
             .HasConstraintName("FK_Categories_Events")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue(EnrolmentStatuses.Registered);

            e.HasIndex(x => new { x.UserID, x.CategoryID })
             .IsUnique()
             .HasDatabaseName("UQ_Enrolments_User_Category");

            e.HasOne(x => x.User)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(x => x.UserID)
             .HasConstraintName("FK_Enrolments_Users")
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryID)
             .HasConstraintName("FK_Enrolments_Categories")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).IsRequired().HasDefaultValue(ResultStatuses.Pending);

            // One enrolment produces at most one result (EnrolmentID is UNIQUE)
            e.HasOne(x => x.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(x => x.EnrolmentID)
             .HasConstraintName("FK_Results_Enrolments")
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
