using Microsoft.EntityFrameworkCore;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskAssignment> Assignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Title).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.State).IsRequired();

            entity.HasOne(x => x.AssignedUser)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Task)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
