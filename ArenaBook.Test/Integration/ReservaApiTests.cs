using System.Net;
using System.Net.Http.Json;
using ArenaBook.Api.Contracts.Requests;
using ArenaBook.Api.Contracts.Responses;
using ArenaBook.Api.Controllers;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArenaBook.Test.Integration;

[Category("Integration")]
[NonParallelizable]
public class ReservaApiTests
{
    private WebApplicationFactory<ReservaController> _factory = null!;
    private HttpClient _client = null!;
    private IServiceScope? _scope;
    private ArenaBookDbContext _context = null!;
    private int _idCliente;
    private int _idQuadra;
    private int _idEsporte;
    private readonly DateOnly _data = new(2030, 1, 6);

    [SetUp]
    public async Task Preparar()
    {
        // Inicia a API dentro do teste, usando os User Secrets de desenvolvimento.
        _idCliente = 0;
        _idQuadra = 0;
        _idEsporte = 0;
        _factory = new WebApplicationFactory<ReservaController>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
        });

        try
        {
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();

            // Cada teste recebe cadastros ficticios exclusivos no banco existente.
            var identificador = Guid.NewGuid().ToString("N");
            var cliente = new Cliente { Nome = "Teste API " + identificador, Telefone = "11999990000", Email = identificador + "@example.invalid" };
            var esporte = new Esporte { Nome = "Esporte teste " + identificador };
            var quadra = new Quadra { Nome = "Quadra teste " + identificador, PrecoHora = 80m, Ativa = true };

            // Se a preparacao falhar, a transacao desfaz os cadastros incompletos.
            await using var transacao = await _context.Database.BeginTransactionAsync();
            _context.AddRange(cliente, esporte, quadra);
            await _context.SaveChangesAsync();
            _idCliente = cliente.IdCliente;
            _idEsporte = esporte.IdEsporte;
            _idQuadra = quadra.IdQuadra;

            _context.QuadraEsportes.Add(new QuadraEsporte { IdQuadra = _idQuadra, IdEsporte = _idEsporte });
            _context.HorariosFuncionamento.Add(new HorarioFuncionamento
            {
                IdQuadra = _idQuadra,
                DiaSemana = (byte)((int)_data.DayOfWeek + 1),
                HoraAbertura = new TimeOnly(8, 0),
                HoraFechamento = new TimeOnly(22, 0)
            });

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch
        {
            _context?.Dispose();
            _scope?.Dispose();
            _client?.Dispose();
            _factory.Dispose();
            throw new InvalidOperationException("Nao foi possivel preparar I01/I02. Confira o SQL Server e ConnectionStrings:ArenaBook nos User Secrets da API.");
        }
    }

    [Test]
    public async Task I01_CriarReservaEConsultarAgenda()
    {
        // Executar: criar a reserva pela API e consultar a mesma quadra e data.
        var reserva = await CriarReservaAsync();
        var agenda = await _client.GetFromJsonAsync<ReservaResponse[]>($"/api/reservas/agenda?idQuadra={_idQuadra}&data={_data:yyyy-MM-dd}");

        // Conferir: identificador, estado e valor aparecem na agenda.
        Assert.That(reserva.IdReserva, Is.GreaterThan(0));
        Assert.That(reserva.Status, Is.EqualTo("Pendente"));
        Assert.That(reserva.ValorTotal, Is.EqualTo(160m));
        Assert.That(agenda, Is.Not.Null.And.Length.EqualTo(1));
        Assert.That(agenda![0].IdReserva, Is.EqualTo(reserva.IdReserva));
        Assert.That(agenda[0].IdQuadra, Is.EqualTo(_idQuadra));
        Assert.That(agenda[0].DataReserva, Is.EqualTo(_data));
        Assert.That(agenda[0].ValorTotal, Is.EqualTo(160m));
    }

    [Test]
    public async Task I02_CancelarReservaLiberaAgenda()
    {
        // Preparar: reserva criada pela API.
        var reserva = await CriarReservaAsync();

        // Executar: cancelar pela API e consultar a agenda novamente.
        using var response = await _client.PatchAsync($"/api/reservas/{reserva.IdReserva}/cancelar", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cancelada = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        var agenda = await _client.GetFromJsonAsync<ReservaResponse[]>($"/api/reservas/agenda?idQuadra={_idQuadra}&data={_data:yyyy-MM-dd}");
        var historico = await _context.Reservas.AsNoTracking().SingleAsync(r => r.IdReserva == reserva.IdReserva);

        // Conferir: sai dos horarios ocupados, mas continua salva como cancelada.
        Assert.That(cancelada, Is.Not.Null);
        Assert.That(cancelada!.IdReserva, Is.EqualTo(reserva.IdReserva));
        Assert.That(cancelada.Status, Is.EqualTo("Cancelada"));
        Assert.That(agenda, Is.Not.Null.And.Empty);
        Assert.That(historico.IdStatus, Is.EqualTo(StatusReserva.Cancelada));
    }

    private async Task<ReservaResponse> CriarReservaAsync()
    {
        var request = new CriarReservaRequest(_idQuadra, _idEsporte, _idCliente, _data, new TimeOnly(18, 0), new TimeOnly(20, 0));
        using var response = await _client.PostAsJsonAsync("/api/reservas", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var reserva = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        Assert.That(reserva, Is.Not.Null);
        return reserva!;
    }

    [TearDown]
    public async Task Limpar()
    {
        try
        {
            // Remove somente os registros criados por este teste, na ordem das FKs.
            await using var transacao = await _context.Database.BeginTransactionAsync();
            await _context.Reservas.Where(r => r.IdQuadra == _idQuadra).ExecuteDeleteAsync();
            await _context.HorariosFuncionamento.Where(h => h.IdQuadra == _idQuadra).ExecuteDeleteAsync();
            await _context.QuadraEsportes.Where(q => q.IdQuadra == _idQuadra).ExecuteDeleteAsync();
            await _context.Quadras.Where(q => q.IdQuadra == _idQuadra).ExecuteDeleteAsync();
            await _context.Clientes.Where(c => c.IdCliente == _idCliente).ExecuteDeleteAsync();
            await _context.Esportes.Where(e => e.IdEsporte == _idEsporte).ExecuteDeleteAsync();
            await transacao.CommitAsync();
        }
        finally
        {
            _context?.Dispose();
            _scope?.Dispose();
            _client.Dispose();
            _factory.Dispose();
        }
    }
}

