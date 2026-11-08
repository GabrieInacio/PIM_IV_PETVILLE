using System.ComponentModel.DataAnnotations;

namespace PetVille.API.Models;

public class Produto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Categoria { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int QuantidadeEstoque { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.Now;
}