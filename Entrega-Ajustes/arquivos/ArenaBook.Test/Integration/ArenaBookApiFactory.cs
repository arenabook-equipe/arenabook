using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArenaBook.Test.Integration;

internal sealed class ArenaBookApiFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; }

    public ArenaBookApiFactory(string connection)
    {
        var configuration = new SqlConnectionStringBuilder(connection)
        {
            InitialCatalog = $"ArenaBook_Tests_{Guid.NewGuid():N}"
        };
        ConnectionString = configuration.ConnectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ArenaBook"] = ConnectionString
            }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
