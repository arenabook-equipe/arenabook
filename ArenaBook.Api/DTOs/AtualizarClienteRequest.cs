using System.ComponentModel.DataAnnotations;

namespace ArenaBook.Api.DTOs;

public class AtualizarClienteRequest
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone.")]
    [StringLength(20, ErrorMessage = "O telefone deve ter até 20 caracteres.")]
    public string Telefone { get; set; } = string.Empty;
}