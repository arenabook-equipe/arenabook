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
[Category("Unit")]
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
}
