
using System.ComponentModel.DataAnnotations;

namespace PetVille.API.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Telefone { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string SenhaHash { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
