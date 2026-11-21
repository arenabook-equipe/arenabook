using System.ComponentModel.DataAnnotations;

namespace ArenaBook.Api.Contracts.Requests;

public record SalvarFuncionamentoRequest([Range(1, 7)] int DiaSemana, [Required] TimeOnly? HoraAbertura, [Required] TimeOnly? HoraFechamento);
