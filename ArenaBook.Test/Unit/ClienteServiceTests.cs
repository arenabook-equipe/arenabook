using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using ArenaBook.Core.Enums;

namespace ArenaBook.Test.Unit;

public class ClienteServiceTests
{
    [Test]
    public async Task U01_CadastroValidoSalvaCliente()
    {
        var repository = new ClienteRepositoryEmMemoria();
        var service = new ClienteService(repository);
        var cliente = await service.CadastrarAsync("Ana Silva", "35999999999", "ana@example.com", CancellationToken.None);

        Assert.That(cliente?.IdCliente, Is.EqualTo(1));
        Assert.That(repository.ClienteSalvo, Is.SameAs(cliente));
        Assert.That(cliente?.Nome, Is.EqualTo("Ana Silva"));
        Assert.That(cliente?.Telefone, Is.EqualTo("35999999999"));
        Assert.That(cliente?.Email, Is.EqualTo("ana@example.com"));
    }
    [Test]
    public void U02_NomeEmBrancoRecusaCadastro()
    {
        var repository = new ClienteRepositoryEmMemoria();
        var service = new ClienteService(repository);

        var erro = Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await service.CadastrarAsync("   ", "35999999999", "ana@example.com", CancellationToken.None);
        });
        Assert.That(erro?.ParamName, Is.EqualTo("nome"));
        Assert.That(repository.ClienteSalvo, Is.Null);
    }
    private sealed class ClienteRepositoryEmMemoria : IClienteRepository
    {
        public Cliente? ClienteSalvo { get; private set; }

        public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(ClienteSalvo?.Email == email);
        }

        public Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
        {
            cliente.IdCliente = 1;
            ClienteSalvo = cliente;
            return Task.CompletedTask;
        }
        public Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken)
        {
            return Task.FromResult(ClienteSalvo?.IdCliente == idCliente ? ClienteSalvo : null);
        }
        public Task<bool> AtualizarAsync(int idCliente, string nome, string telefone, CancellationToken cancellationToken)
        {
            var cliente = ClienteSalvo;

            if (cliente is null || cliente.IdCliente != idCliente)
            {
                return Task.FromResult(false);
            }

            cliente.Nome = nome;
            cliente.Telefone = telefone;

            return Task.FromResult(true);
        }
        public Task<ResultadoExclusaoCliente> ExcluirAsync(int idCliente, CancellationToken cancellationToken)
        {
            var cliente = ClienteSalvo;

            if (cliente is null || cliente.IdCliente != idCliente)
            {
                return Task.FromResult(ResultadoExclusaoCliente.NaoEncontrado);
            }

            if (cliente.Reservas.Count > 0)
            {
                return Task.FromResult(ResultadoExclusaoCliente.PossuiReservas);
            }

            ClienteSalvo = null;
            return Task.FromResult(ResultadoExclusaoCliente.Excluido);
        }
    }

}