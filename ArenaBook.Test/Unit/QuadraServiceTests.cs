using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using NUnit.Framework;

namespace ArenaBook.Test.Unit;

public class FakeQuadraRepository : IQuadraCadastroRepository
{
    public List<Quadra> Quadras { get; } = new();
    public HashSet<int> EsportesExistentes { get; } = new() { 1, 2, 3 };

    public Task<bool> TodosEsportesExistemAsync(IReadOnlyCollection<int> idsEsportes) =>
    Task.FromResult(idsEsportes.All(EsportesExistentes.Contains));

    public Task<Quadra> AdicionarAsync(Quadra quadra)
    {
        quadra.IdQuadra = Quadras.Count + 1;
        Quadras.Add(quadra);
        return Task.FromResult(quadra);
    }

    public Task<Quadra?> ObterPorIdAsync(int idQuadra) =>
        Task.FromResult(Quadras.FirstOrDefault(q => q.IdQuadra == idQuadra));

    public Task AtualizarAsync(Quadra quadra) => Task.CompletedTask;
}

[TestFixture]
public class QuadraServiceTests
{
    private FakeQuadraRepository _repositorio = null!;
    private QuadraService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositorio = new FakeQuadraRepository();
        _service = new QuadraService(_repositorio);
    }

    [Test]
    public async Task Q01_CadastroValido_QuadraPoliesportiva_RegistraModalidadesEPreco()
    {
        // ids 1, 2 e 3 representam futsal, basquete e vôlei
        var quadra = await _service.CadastrarAsync("Quadra 1", new[] { 1, 2, 3 }, 80m);

        Assert.That(quadra.IdQuadra, Is.EqualTo(1));
        Assert.That(quadra.Nome, Is.EqualTo("Quadra 1"));
        Assert.That(quadra.PrecoHora, Is.EqualTo(80m));
        Assert.That(quadra.QuadraEsportes.Select(qe => qe.IdEsporte), Is.EquivalentTo(new[] { 1, 2, 3 }));
        Assert.That(_repositorio.Quadras, Has.Count.EqualTo(1));
    }

    [Test]
    public void Q02_CadastroComPrecoZero_EhRecusadoEQuadraNaoEhIncluida()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("Quadra 1", new[] { 1 }, 0m));

        Assert.That(_repositorio.Quadras, Is.Empty);
    }
    [Test]
    public void CadastroComNomeVazio_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("", new[] { 1 }, 80m));
        Assert.That(_repositorio.Quadras, Is.Empty);
    }

    [Test]
    public void CadastroSemModalidades_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("Quadra 1", Array.Empty<int>(), 80m));
        Assert.That(_repositorio.Quadras, Is.Empty);
    }

    [Test]
    public void CadastroComModalidadeInexistente_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("Quadra 1", new[] { 99 }, 80m));
        Assert.That(_repositorio.Quadras, Is.Empty);
    }

    [Test]
    public void CadastroComPrecoAcimaDoLimite_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("Quadra 1", new[] { 1 }, 100_000_000m));
    }

    [Test]
    public void CadastroComMaisDeDuasCasasDecimais_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.CadastrarAsync("Quadra 1", new[] { 1 }, 80.555m));
    }

    [Test]
    public async Task ConsultarQuadraCadastrada_RetornaOsDados()
    {
        var criada = await _service.CadastrarAsync("Quadra 1", new[] { 1, 2 }, 80m);

        var consultada = await _service.ConsultarAsync(criada.IdQuadra);

        Assert.That(consultada.Nome, Is.EqualTo("Quadra 1"));
        Assert.That(consultada.PrecoHora, Is.EqualTo(80m));
    }

    [Test]
    public void ConsultarQuadraInexistente_GeraErro()
    {
        Assert.ThrowsAsync<QuadraNaoEncontradaException>(() => _service.ConsultarAsync(999));
    }

    [Test]
    public async Task EditarQuadra_AtualizaNomePrecoEModalidades()
    {
        var quadra = await _service.CadastrarAsync("Quadra 1", new[] { 1 }, 80m);

        var editada = await _service.EditarAsync(quadra.IdQuadra, "Quadra Central", new[] { 2, 3 }, 100m);

        Assert.That(editada.Nome, Is.EqualTo("Quadra Central"));
        Assert.That(editada.PrecoHora, Is.EqualTo(100m));
        Assert.That(editada.QuadraEsportes.Select(qe => qe.IdEsporte), Is.EquivalentTo(new[] { 2, 3 }));
    }

    [Test]
    public async Task EditarComDadoInvalido_EhRecusadoENaoAlteraAQuadra()
    {
        var quadra = await _service.CadastrarAsync("Quadra 1", new[] { 1 }, 80m);

        Assert.ThrowsAsync<QuadraInvalidaException>(() =>
            _service.EditarAsync(quadra.IdQuadra, null, null, 0m));

        Assert.That((await _service.ConsultarAsync(quadra.IdQuadra)).PrecoHora, Is.EqualTo(80m));
    }

    [Test]
    public async Task QuadraDesativada_FicaInativa()
    {
        var quadra = await _service.CadastrarAsync("Quadra 1", new[] { 1 }, 80m);

        await _service.DesativarAsync(quadra.IdQuadra);

        Assert.That(await _service.EstaDisponivelAsync(quadra.IdQuadra), Is.False);
    }

    [Test]
    public void DesativarQuadraInexistente_GeraErro()
    {
        Assert.ThrowsAsync<QuadraNaoEncontradaException>(() => _service.DesativarAsync(999));
    }

    [Test]
    public void CadastroComNomeAcimaDe100Caracteres_EhRecusado()
    {
        Assert.ThrowsAsync<QuadraInvalidaException>(() => _service.CadastrarAsync(new string('a', 101), new[] { 1 }, 80m));
        Assert.That(_repositorio.Quadras, Is.Empty);
    }
}