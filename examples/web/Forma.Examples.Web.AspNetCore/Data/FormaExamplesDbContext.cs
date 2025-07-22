using Microsoft.EntityFrameworkCore;
using Forma.Examples.Web.AspNetCore.Data.Entities;

namespace Forma.Examples.Web.AspNetCore.Data;

/// <summary>
/// Database context for the Forma examples application
/// </summary>
public class FormaExamplesDbContext : DbContext
{
    public FormaExamplesDbContext(DbContextOptions<FormaExamplesDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Seed some initial data
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow }
        );
    }
}