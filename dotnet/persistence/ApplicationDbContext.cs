using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using model.Core;

namespace persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Ad> Ads { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ad>().Property(a => a.AdScope).HasConversion<string>();
    }
}

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) =>
        CreateDbContext(CONNECTION_STRING);

    public ApplicationDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(
            connectionString
        );
        return new ApplicationDbContext(options.Options);
    }

    public static string CONNECTION_STRING =>
        Environment.GetEnvironmentVariable("CONNECTION_STRING")!;
}
