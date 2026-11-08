using System.ComponentModel.DataAnnotations;

namespace PetVille.API.Models;

public class Agendamento
{
    public int Id { get; set; }

    public DateTime DataHora { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pendente";

    [MaxLength(500)]
    public string Observacao { get; set; } = string.Empty;

    // Relacionamento com o cliente
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    // Relacionamento com o pet
    public int PetId { get; set; }
    public Pet? Pet { get; set; }

    // Relacionamento com o serviço
    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }
}