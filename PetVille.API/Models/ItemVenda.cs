using System.ComponentModel.DataAnnotations.Schema;

namespace PetVille.API.Models;

public class ItemVenda
{
    public int Id { get; set; }

    public int Quantidade { get; set; }

    // Preco registrado no momento da venda
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }

    // Relacionamento com Venda
    public int VendaId { get; set; }
    public Venda? Venda { get; set; }

    // Relacionamento com Produto
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
}