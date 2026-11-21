from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Entrega-Ajustes' / 'arquivos'
OUT.mkdir(parents=True, exist_ok=True)

def write(name, content):
    target = OUT / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding='utf-8')

def original(name):
    return (ROOT / name).read_text(encoding='utf-8-sig')

copies = [
    'ArenaBook.Api/ArenaBook.Api.csproj',
    'ArenaBook.Api/Controllers/ReservaController.cs',
    'ArenaBook.Api/Controllers/HorarioFuncionamentoController.cs',
    'ArenaBook.Api/DTOs/HorarioFuncionamentoDtos.cs',
    'ArenaBook.Core/Interfaces/IReservaService.cs',
    'ArenaBook.Core/Interfaces/IReservaRepository.cs',
    'ArenaBook.Core/Interfaces/IHorarioFuncionamentoRepository.cs',
    'ArenaBook.Core/Services/HorarioFuncionamentoService.cs',
    'ArenaBook.Infrastructure/Repositories/ReservaRepository.cs',
    'ArenaBook.Infrastructure/Repositories/HorarioFuncionamentoRepository.cs',
]
for name in copies:
    write(name, original(name))

program = original('ArenaBook.Api/Program.cs')
program = program.replace('AddScoped< IHorarioFuncionamentoRepository', 'AddScoped<IHorarioFuncionamentoRepository')
program += '\n// Permite iniciar a API nos testes HTTP com WebApplicationFactory.\npublic partial class Program { }\n'
write('ArenaBook.Api/Program.cs', program)

write('ArenaBook.Core/Exceptions/ConflitoReservaException.cs', '''namespace ArenaBook.Core.Exceptions;

public sealed class ConflitoReservaException(string mensagem) : Exception(mensagem);
''')

service = original('ArenaBook.Core/Services/ReservaService.cs')
service = service.replace('throw new InvalidOperationException("A reserva não está mais ativa para cancelamento.");',
                          'throw new ConflitoReservaException("A reserva não está mais ativa para cancelamento.");')
marker = '        if (horaInicio.Ticks % TimeSpan.TicksPerHour != 0 || horaFim.Ticks % TimeSpan.TicksPerHour != 0)'
assert marker in service
service = service.replace(marker, '''        if (dataReserva == default)
        {
            throw new ArgumentException("Informe uma data válida.");
        }

''' + marker)
write('ArenaBook.Core/Services/ReservaService.cs', service)

contracts = original('ArenaBook.Api/Contracts/ReservaContracts.cs')
contracts = contracts[contracts.index('namespace ArenaBook.Api.Contracts;'):]
write('ArenaBook.Api/Contracts/ReservaContracts.cs', 'using ArenaBook.Core.Entities;\n\n' + contracts)

project = original('ArenaBook.Test/ArenaBook.Test.csproj')
project = project.replace('    <PackageReference Include="coverlet.collector"',
'''    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.13" />
    <PackageReference Include="coverlet.collector"''')
write('ArenaBook.Test/ArenaBook.Test.csproj', project)

tests = original('ArenaBook.Test/Unit/ReservaServiceTests.cs')
tests = tests.replace('Assert.ThrowsAsync<InvalidOperationException>', 'Assert.ThrowsAsync<ConflitoReservaException>')
patterns = {
    '[TestCase(99, 2, true, 18, 0, 20, typeof(InvalidOperationException))]': 'KeyNotFoundException',
    '[TestCase(1, 2, false, 18, 0, 20, typeof(InvalidOperationException))]': 'ConflitoReservaException',
    '[TestCase(1, 99, true, 18, 0, 20, typeof(InvalidOperationException))]': 'ArgumentException',
    '[TestCase(1, 2, true, 7, 0, 9, typeof(InvalidOperationException))]': 'ArgumentException',
    '[TestCase(1, 2, true, 21, 0, 23, typeof(InvalidOperationException))]': 'ArgumentException',
}
for text, exception in patterns.items():
    assert text in tests, text
    tests = tests.replace(text, text.replace('InvalidOperationException', exception))
for args, exception in [
    ('99, 1, 2, true, 18, 0, 20, 0', 'KeyNotFoundException'),
    ('1, 99, 2, true, 18, 0, 20, 0', 'KeyNotFoundException'),
    ('1, 1, 2, false, 18, 0, 20, 0', 'ConflitoReservaException'),
    ('1, 1, 99, true, 18, 0, 20, 0', 'ArgumentException'),
    ('1, 1, 2, true, 7, 0, 9, 0', 'ArgumentException'),
    ('1, 1, 2, true, 21, 0, 23, 0', 'ArgumentException'),
]:
    old = f'[TestCase({args},\n        typeof(InvalidOperationException))]'
    assert old in tests, old
    tests = tests.replace(old, old.replace('InvalidOperationException', exception))
start = tests.index('    private sealed class ClienteRepositoryFake')
end = tests.index('    private sealed class QuadraRepositoryFake', start)
tests = tests[:start] + '''    private sealed class ClienteRepositoryFake : IClienteRepository
    {
        public Task<Cliente?> ObterPorIdAsync(int idCliente, CancellationToken cancellationToken)
        {
            Cliente? cliente = idCliente == 1
                ? new Cliente { IdCliente = 1, Nome = "Cliente de teste" }
                : null;
            return Task.FromResult(cliente);
        }

        public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> AtualizarAsync(int idCliente, string nome, string telefone, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ResultadoExclusaoCliente> ExcluirAsync(int idCliente, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

''' + tests[end:]
start = tests.index('    public void Cancelamento_ReservaInexistente_DeveSerRecusado()')
end = tests.index('    [Test]', start)
tests = tests[:start] + tests[start:end].replace('Assert.ThrowsAsync<ConflitoReservaException>', 'Assert.ThrowsAsync<KeyNotFoundException>') + tests[end:]
start = tests.index('    public void Sugestoes_QuadraInexistenteOuDesativada')
end = tests.index('    private static Quadra CriarQuadraParaSugestoes', start)
tests = tests[:start] + tests[start:end].replace('Assert.ThrowsAsync<ConflitoReservaException>(async () =>',
    'Assert.ThrowsAsync(idQuadra == 99 ? typeof(KeyNotFoundException) : typeof(ConflitoReservaException), async () =>') + tests[end:]
query = '''
        public Task<IReadOnlyList<Reserva>> ConsultarAgendaAsync(int idQuadra, DateOnly dataReserva, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Reserva> agenda = ReservasSalvas
                .Where(r => r.IdQuadra == idQuadra && r.DataReserva == dataReserva &&
                    (r.IdStatus == StatusReserva.Pendente || r.IdStatus == StatusReserva.Confirmada))
                .OrderBy(r => r.HoraInicio)
                .ToList();
            return Task.FromResult(agenda);
        }
'''
last = tests.rfind('    }')
tests = tests[:last] + query + tests[last:]
write('ArenaBook.Test/Unit/ReservaServiceTests.cs', tests)
