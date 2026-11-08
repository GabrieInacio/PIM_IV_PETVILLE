
using System.ComponentModel.DataAnnotations;

namespace PetVille.API.Models;

public class Pet
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Especie { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Raca { get; set; } = string.Empty;

    public DateTime? DataNascimento { get; set; }

    [MaxLength(20)]
    public string Sexo { get; set; } = string.Empty;

    // Relacionamento com Cliente
    public int ClienteId { get; set; }

    public Cliente? Cliente { get; set; }
}
