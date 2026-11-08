
using Microsoft.EntityFrameworkCore;
using PetVille.API.Models;

namespace PetVille.API.Data;

public class PetVilleContext : DbContext
{
    public PetVilleContext(DbContextOptions<PetVilleContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }

    public DbSet<Pet> Pets { get; set; }

    public DbSet<Servico> Servicos { get; set; }

    public DbSet<Agendamento> Agendamentos { get; set; }

    public DbSet<Produto> Produtos { get; set; }

    public DbSet<Funcionario> Funcionarios { get; set; }

    public DbSet<Venda> Vendas { get; set; }

    public DbSet<ItemVenda> ItensVenda { get; set; }
    
}
