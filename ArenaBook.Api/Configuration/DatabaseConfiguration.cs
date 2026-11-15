using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Api.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddArenaBookDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArenaBook");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:ArenaBook nas configurações da aplicação (User Secrets em desenvolvimento).");

        services.AddDbContext<ArenaBookDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}

