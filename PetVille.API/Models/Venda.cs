using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetVille.API.Models;

public class Venda
{
    public int Id { get; set; }

    public DateTime DataVenda { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }

    // Funcionario responsavel pela venda
    public int FuncionarioId { get; set; }
    public Funcionario? Funcionario { get; set; }

    // Produtos que fazem parte da venda
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}