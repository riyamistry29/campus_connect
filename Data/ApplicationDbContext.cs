using CampusConnect.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventCategory> Categories { get; set; } = null!;
    public DbSet<CollegeEvent> Events { get; set; } = null!;
    public DbSet<EventRegistration> Registrations { get; set; } = null!;
    public DbSet<EventAnnouncement> Announcements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clean database naming
        builder.Entity<ApplicationUser>(entity => entity.ToTable("Users"));

        // EventCategory
        builder.Entity<EventCategory>(entity =>
        {
            entity.ToTable("EventCategories");
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // CollegeEvent
        builder.Entity<CollegeEvent>(entity =>
        {
            entity.ToTable("CollegeEvents");
            entity.HasIndex(e => new { e.Status, e.StartDateTime });
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.OrganizerId);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Events)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Organizer)
                  .WithMany(u => u.OrganizedEvents)
                  .HasForeignKey(e => e.OrganizerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // EventRegistration
        builder.Entity<EventRegistration>(entity =>
        {
            entity.ToTable("EventRegistrations");

            // Unique constraint: one student per event
            entity.HasIndex(r => new { r.EventId, r.StudentId }).IsUnique();
            entity.HasIndex(r => r.StudentId);
            entity.HasIndex(r => r.Status);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Registrations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Student)
                  .WithMany(u => u.Registrations)
                  .HasForeignKey(r => r.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // EventAnnouncement
        builder.Entity<EventAnnouncement>(entity =>
        {
            entity.ToTable("EventAnnouncements");
            entity.HasIndex(a => a.EventId);

            entity.HasOne(a => a.Event)
                  .WithMany(e => e.Announcements)
                  .HasForeignKey(a => a.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.CreatedBy)
                  .WithMany()
                  .HasForeignKey(a => a.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
