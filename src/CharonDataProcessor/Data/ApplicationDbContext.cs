using CharonDataProcessor.Models;
using Microsoft.EntityFrameworkCore;

namespace CharonDataProcessor.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Metric> Metrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

