using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using persistence.Repository;

namespace persistence;

public static class ApplicationDbContextExtensions
{
    public static void AddSotexBoxDatabase(this IServiceCollection services)
    {
        services.AddDbContextPool<ApplicationDbContext>(options =>
            options.UseNpgsql(ApplicationDbContextFactory.CONNECTION_STRING)
        );

        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAdRepository, AdRepository>();
    }
}
