using Microsoft.EntityFrameworkCore;

namespace storage;

public class MemeDbContext : DbContext
{
    public MemeDbContext(DbContextOptions<MemeDbContext> options) : base(options)
    {
    }

    public DbSet<Meme> Meme => Set<Meme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Meme>(e =>
        {
            e.HasKey(m => m.Name);
            e.Property(m => m.Data).IsRequired();
        });

    }
}

