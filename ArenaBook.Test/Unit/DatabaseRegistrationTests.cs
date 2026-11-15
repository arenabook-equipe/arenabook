using ArenaBook.Api.Configuration;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaBook.Test.Unit;

public class DatabaseRegistrationTests
{
    [Test]
    public void Registration_UsesConfiguredConnectionAndSqlServerWithoutConnecting()
    {
        const string connectionString = "Server=localhost;Database=ConfigurationTest;Integrated Security=true;TrustServerCertificate=true";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:ArenaBook"] = connectionString }).Build();
        var services = new ServiceCollection();
        services.AddArenaBookDatabase(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
        Assert.That(context.Database.ProviderName, Is.EqualTo("Microsoft.EntityFrameworkCore.SqlServer"));
        Assert.That(context.Database.GetDbConnection().DataSource, Is.EqualTo("localhost"));
        Assert.That(context.Database.GetDbConnection().Database, Is.EqualTo("ConfigurationTest"));
        Assert.That(provider.GetRequiredService<IServiceScopeFactory>(), Is.Not.Null);
    }

    [Test]
    public void Registration_MissingConnectionStringFailsWithConfigurationKey()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.That(() => new ServiceCollection().AddArenaBookDatabase(configuration),
            Throws.InvalidOperationException.With.Message.Contains("ConnectionStrings:ArenaBook"));
    }
}


