using ArenaBook.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Api.Diagnostics;

// Diagnostico temporario: chamado somente pelo comando de desenvolvimento, sem iniciar HTTP.
internal static class DatabaseConnectionCheck
{
    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
        var stage = "abrir conexão";
        try
        {
            await database.Database.OpenConnectionAsync(timeout.Token);
            Console.WriteLine("Conexão com SQL Server confirmada.");

            stage = "confirmar o banco selecionado";
            var databaseName = await database.Database
                .SqlQuery<string>($"SELECT DB_NAME() AS [Value]")
                .SingleAsync(timeout.Token);
            if (!string.Equals(databaseName, "ArenaBook", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Falha: a conexão não selecionou o banco ArenaBook.");
                return 1;
            }
            Console.WriteLine("Banco ArenaBook acessível.");

            stage = "consultar a tabela Clientes";
            await database.Clientes.AsNoTracking()
                .Select(cliente => cliente.IdCliente)
                .Take(1)
                .ToListAsync(timeout.Token);
            Console.WriteLine("Consulta somente de leitura em Clientes executada com sucesso (tabela vazia também é válida).");
            return 0;
        }
        catch (SqlException exception)
        {
            // Nao imprimir a excecao completa: ela pode conter detalhes da conexao.
            Console.Error.WriteLine($"Falha ao {stage}. Código SQL Server: {exception.Number}.");
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine($"Tempo limite de 30 segundos ao {stage}.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Falha ao {stage}. Tipo: {exception.GetType().Name}.");
            return 1;
        }
    }
}

