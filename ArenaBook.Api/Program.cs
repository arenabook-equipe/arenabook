using ArenaBook.Api.Configuration;
using ArenaBook.Api.Diagnostics;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using ArenaBook.Infrastructure.Repositories;

var checkDatabase = args.Contains("--check-database", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(
    args.Where(argument => argument != "--check-database").ToArray());

if (checkDatabase)
{
    // O diagnostico emite somente resultados controlados, sem logs de conexao do provedor.
    builder.Logging.ClearProviders();
    if (!builder.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("--check-database só pode ser executado em Development.");
        Environment.ExitCode = 1;
        return;
    }
}

builder.Services.AddArenaBookDatabase(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IQuadraCadastroRepository, QuadraCadastroRepository>();
builder.Services.AddScoped<QuadraService>();
builder.Services.AddScoped<IQuadraDisponibilidade>(sp => sp.GetRequiredService<QuadraService>());

var app = builder.Build();

if (checkDatabase)
{
    Environment.ExitCode = await DatabaseConnectionCheck.RunAsync(app.Services);
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

