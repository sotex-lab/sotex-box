using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using model.Core;
using persistence.Converters;
using Persistence.Repository.Base;

namespace persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<Ad> Ads { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Configuration> Configurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ad>().Property(a => a.AdScope).HasConversion<string>();
        modelBuilder.Entity<Ad>().Navigation(n => n.Tags).AutoInclude();

        modelBuilder
            .Entity<Device>()
            .Property(d => d.CreatedDate)
            .HasConversion<DateTimeValueConverter>();
        modelBuilder.Entity<Device>().Property(d => d.Ip).HasConversion<IpAddressValueConverter>();
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
