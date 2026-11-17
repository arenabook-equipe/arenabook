using System;
using System.Threading;
using System.Threading.Tasks;
using ArenaBook.Core.Entities;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Enums;

namespace ArenaBook.Core.Services;

public class ClienteService
{
    private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Cliente?> CadastrarAsync(
        string nome,
        string telefone,
        string email,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(telefone);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var emailCadastro = email.Trim();

        if (await _repository.ExistePorEmailAsync(emailCadastro, cancellationToken))
        {
            return null;
        }

        var cliente = new Cliente
        {
            Nome = nome.Trim(),
            Telefone = telefone.Trim(),
            Email = emailCadastro
        };

        await _repository.AdicionarAsync(cliente, cancellationToken);

        return cliente;
    }
    public Task<Cliente?> ObterPorIdAsync(
    int idCliente,
    CancellationToken cancellationToken)
    {
        return _repository.ObterPorIdAsync(idCliente, cancellationToken);
    }
    public Task<bool> AtualizarAsync(
    int idCliente,
    string nome,
    string telefone,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(telefone);

        return _repository.AtualizarAsync(
            idCliente,
            nome.Trim(),
            telefone.Trim(),
            cancellationToken);
    }
    public Task<ResultadoExclusaoCliente> ExcluirAsync(
    int idCliente,
    CancellationToken cancellationToken)
    {
        return _repository.ExcluirAsync(idCliente, cancellationToken);
    }
}