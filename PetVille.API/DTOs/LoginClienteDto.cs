using System.ComponentModel.DataAnnotations;

namespace PetVille.API.DTOs;

public class LoginClienteDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}