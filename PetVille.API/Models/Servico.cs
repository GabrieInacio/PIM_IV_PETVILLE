using System.ComponentModel.DataAnnotations;

namespace PetVille.API.Models;

public class Servico
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Descricao { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int DuracaoMinutos { get; set; }

    public bool Ativo { get; set; } = true;
}