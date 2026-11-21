using System.Net;
using System.Net.Http.Json;
using ArenaBook.Api.Contracts;
using ArenaBook.Api.DTOs;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaBook.Test.Integration;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ReservasFuncionamentoHttpTests
{
    private ArenaBookApiFactory? _factory;
    private HttpClient _client = null!;
    private bool _bancoCriado;
    private int _idCliente;
    private int _idQuadra;
    private int _idEsporte;
    private int _idHorario;
    private static readonly DateOnly Data = new(2030, 1, 6);

    [SetUp]
    public async Task Preparar()
    {
        _factory = null;
        _bancoCriado = false;
        var connection = Environment.GetEnvironmentVariable("ARENABOOK_TEST_SQL");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Configure ARENABOOK_TEST_SQL para executar os testes com SQL Server.");
            return;
        }

        _factory = new ArenaBookApiFactory(connection);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
        await db.Database.EnsureCreatedAsync();
        _bancoCriado = true;

        var cliente = new Cliente
        {
            Nome = "Cliente integração",
            Telefone = "35999999999",
            Email = "integracao@example.com"
        };
        var esporte = new Esporte { Nome = "Futsal" };
        var quadra = new Quadra { Nome = "Quadra integração", PrecoHora = 80m, Ativa = true };
        db.AddRange(cliente, esporte, quadra);
        await db.SaveChangesAsync();
        db.QuadraEsportes.Add(new QuadraEsporte
        {
            IdQuadra = quadra.IdQuadra,
            IdEsporte = esporte.IdEsporte
        });
        await db.SaveChangesAsync();
        _idCliente = cliente.IdCliente;
        _idQuadra = quadra.IdQuadra;
        _idEsporte = esporte.IdEsporte;

        using var response = await _client.PostAsJsonAsync(FuncionamentoUrl,
            new SalvarFuncionamentoRequest(1, new TimeOnly(8, 0), new TimeOnly(22, 0)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var horario = await response.Content.ReadFromJsonAsync<HorarioFuncionamentoResponse>();
        _idHorario = horario!.IdHorario;
    }

    [TearDown]
    public async Task Limpar()
    {
        if (_factory is null) return;
        try
        {
            if (_bancoCriado)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
                var nome = db.Database.GetDbConnection().Database;
                if (!nome.StartsWith("ArenaBook_Tests_", StringComparison.Ordinal))
                    throw new InvalidOperationException("O banco temporário possui nome inesperado.");
                await db.Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }
    }

    private string FuncionamentoUrl => $"/api/quadras/{_idQuadra}/funcionamento";

    private async Task<ReservaResponse> CriarReservaAsync(int inicio = 18, int fim = 20)
    {
        using var response = await _client.PostAsJsonAsync("/api/reservas",
            new CriarReservaRequest(_idQuadra, _idEsporte, _idCliente, Data,
                new TimeOnly(inicio, 0), new TimeOnly(fim, 0)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return (await response.Content.ReadFromJsonAsync<ReservaResponse>())!;
    }

    private async Task<ReservaResponse[]> AgendaAsync()
    {
        using var response = await _client.GetAsync(
            $"/api/reservas/agenda?idQuadra={_idQuadra}&data=2030-01-06");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return (await response.Content.ReadFromJsonAsync<ReservaResponse[]>())!;
    }

    private async Task<HorarioFuncionamentoResponse[]> FuncionamentoAsync()
    {
        using var response = await _client.GetAsync(FuncionamentoUrl);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return (await response.Content.ReadFromJsonAsync<HorarioFuncionamentoResponse[]>())!;
    }

    [Test]
    public async Task I01_CriarPelaApi_DeveAparecerNaAgenda()
    {
        var criada = await CriarReservaAsync();
        var agenda = await AgendaAsync();
        Assert.Multiple(() =>
        {
            Assert.That(criada.IdReserva, Is.GreaterThan(0));
            Assert.That(criada.Status, Is.EqualTo("Pendente"));
            Assert.That(criada.ValorTotal, Is.EqualTo(160m));
            Assert.That(agenda, Has.Length.EqualTo(1));
            Assert.That(agenda.Single(), Is.EqualTo(criada));
        });
    }

    [TestCase(StatusReserva.Pendente)]
    [TestCase(StatusReserva.Confirmada)]
    public async Task I02_CancelarPelaApi_SaiDaAgendaEPreservaHistorico(StatusReserva status)
    {
        var criada = await CriarReservaAsync();
        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
            await db.Reservas.Where(r => r.IdReserva == criada.IdReserva)
                .ExecuteUpdateAsync(update => update.SetProperty(r => r.IdStatus, status));
        }
        Assert.That((await AgendaAsync()).Select(r => r.IdReserva), Does.Contain(criada.IdReserva));
        using var response = await _client.PatchAsync($"/api/reservas/{criada.IdReserva}/cancelar", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var cancelada = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        Assert.That(cancelada!.Status, Is.EqualTo("Cancelada"));
        Assert.That((await AgendaAsync()).Select(r => r.IdReserva), Does.Not.Contain(criada.IdReserva));

        await using (var scope = _factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
            var salva = await db.Reservas.AsNoTracking().SingleAsync(r => r.IdReserva == criada.IdReserva);
            Assert.That(salva.IdStatus, Is.EqualTo(StatusReserva.Cancelada));
        }
        var nova = await CriarReservaAsync();
        Assert.That(nova.IdReserva, Is.Not.EqualTo(criada.IdReserva));
    }

    [Test]
    public async Task Agenda_FiltraQuadraDataStatusEOrdena()
    {
        var tarde = await CriarReservaAsync(18, 20);
        var cedo = await CriarReservaAsync(10, 11);
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ArenaBookDbContext>();
        await db.Reservas.Where(r => r.IdReserva == cedo.IdReserva)
            .ExecuteUpdateAsync(update => update.SetProperty(r => r.IdStatus, StatusReserva.Confirmada));
        var outraQuadra = new Quadra { Nome = "Outra quadra", PrecoHora = 80m, Ativa = true };
        db.Quadras.Add(outraQuadra);
        await db.SaveChangesAsync();
        db.QuadraEsportes.Add(new QuadraEsporte { IdQuadra = outraQuadra.IdQuadra, IdEsporte = _idEsporte });
        await db.SaveChangesAsync();
        Reserva Extra(int quadra, DateOnly data, StatusReserva status) => new()
        {
            IdCliente = _idCliente, IdQuadra = quadra, IdEsporte = _idEsporte,
            DataReserva = data, HoraInicio = new TimeOnly(12, 0), HoraFim = new TimeOnly(13, 0),
            ValorTotal = 80m, IdStatus = status
        };
        db.Reservas.AddRange(
            Extra(_idQuadra, Data, StatusReserva.Cancelada),
            Extra(_idQuadra, Data, StatusReserva.Concluida),
            Extra(_idQuadra, Data.AddDays(1), StatusReserva.Pendente),
            Extra(outraQuadra.IdQuadra, Data, StatusReserva.Pendente));
        await db.SaveChangesAsync();
        var agenda = await AgendaAsync();
        Assert.That(agenda.Select(r => r.IdReserva), Is.EqualTo(new[] { cedo.IdReserva, tarde.IdReserva }));
    }

    [Test]
    public async Task Funcionamento_CadastrarEConsultar()
    {
        using var response = await _client.PostAsJsonAsync(FuncionamentoUrl,
            new SalvarFuncionamentoRequest(2, new TimeOnly(9, 0), new TimeOnly(21, 0)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var criado = await response.Content.ReadFromJsonAsync<HorarioFuncionamentoResponse>();
        var horarios = await FuncionamentoAsync();
        Assert.That(horarios.Single(h => h.IdHorario == criado!.IdHorario), Is.EqualTo(criado));
    }

    [Test]
    public async Task Funcionamento_AlteracaoValida_Persiste()
    {
        using var response = await _client.PutAsJsonAsync($"{FuncionamentoUrl}/{_idHorario}",
            new SalvarFuncionamentoRequest(1, new TimeOnly(10, 0), new TimeOnly(21, 0)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var atual = (await FuncionamentoAsync()).Single(h => h.IdHorario == _idHorario);
        Assert.Multiple(() =>
        {
            Assert.That(atual.HoraAbertura, Is.EqualTo(new TimeOnly(10, 0)));
            Assert.That(atual.HoraFechamento, Is.EqualTo(new TimeOnly(21, 0)));
        });
        // A reserva deve usar a configuração alterada.
        using var fora = await _client.PostAsJsonAsync("/api/reservas",
            new CriarReservaRequest(_idQuadra, _idEsporte, _idCliente, Data,
                new TimeOnly(8, 0), new TimeOnly(9, 0)));
        Assert.That(fora.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase(0, 8, 22)]
    [TestCase(8, 8, 22)]
    [TestCase(1, 22, 8)]
    [TestCase(1, 8, 8)]
    [TestCase(1, 8, 23)]
    public async Task Funcionamento_AlteracaoInvalida_PreservaAnterior(int dia, int inicio, int fim)
    {
        using var cadastro = await _client.PostAsJsonAsync(FuncionamentoUrl,
            new SalvarFuncionamentoRequest(1, new TimeOnly(22, 0), new TimeOnly(23, 0)));
        Assert.That(cadastro.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var antes = (await FuncionamentoAsync()).Single(h => h.IdHorario == _idHorario);
        using var response = await _client.PutAsJsonAsync($"{FuncionamentoUrl}/{_idHorario}",
            new SalvarFuncionamentoRequest(dia, new TimeOnly(inicio, 0), new TimeOnly(fim, 0)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var depois = (await FuncionamentoAsync()).Single(h => h.IdHorario == _idHorario);
        Assert.That(depois, Is.EqualTo(antes));
    }

    [TestCase(0, HttpStatusCode.BadRequest)]
    [TestCase(int.MaxValue, HttpStatusCode.NotFound)]
    public async Task Cancelamento_IdInvalidoOuInexistente(int id, HttpStatusCode esperado)
    {
        using var response = await _client.PatchAsync($"/api/reservas/{id}/cancelar", null);
        Assert.That(response.StatusCode, Is.EqualTo(esperado));
    }

    [Test]
    public async Task Reserva_SobreposicaoEReservaJaCancelada_Retornam409()
    {
        var criada = await CriarReservaAsync();
        using var conflito = await _client.PostAsJsonAsync("/api/reservas",
            new CriarReservaRequest(_idQuadra, _idEsporte, _idCliente, Data,
                new TimeOnly(19, 0), new TimeOnly(21, 0)));
        Assert.That(conflito.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using var primeira = await _client.PatchAsync($"/api/reservas/{criada.IdReserva}/cancelar", null);
        Assert.That(primeira.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var segunda = await _client.PatchAsync($"/api/reservas/{criada.IdReserva}/cancelar", null);
        Assert.That(segunda.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }
}
