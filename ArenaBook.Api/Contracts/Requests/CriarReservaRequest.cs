using System.ComponentModel.DataAnnotations;

namespace ArenaBook.Api.Contracts.Requests;

public record CriarReservaRequest([Range(1, int.MaxValue)] int IdQuadra, [Range(1, int.MaxValue)] int IdEsporte, [Range(1, int.MaxValue)] int IdCliente, [Required] DateOnly? DataReserva, [Required] TimeOnly? HoraInicio, [Required] TimeOnly? HoraFim);
