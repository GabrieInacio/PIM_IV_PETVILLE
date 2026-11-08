using Microsoft.EntityFrameworkCore;
using PetVille.API.Models;

namespace PetVille.API.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(PetVilleContext db)
    {
        await db.Database.MigrateAsync();
        if (!await db.Servicos.AnyAsync())
        {
            db.Servicos.AddRange(
                new Servico { Nome = "Banho", Descricao = "Banho completo", Preco = 50, DuracaoMinutos = 60 },
                new Servico { Nome = "Tosa", Descricao = "Tosa higiênica", Preco = 65, DuracaoMinutos = 60 },
                new Servico { Nome = "Banho e tosa", Descricao = "Pacote completo", Preco = 100, DuracaoMinutos = 90 });
            await db.SaveChangesAsync();
        }
        if (!await db.Produtos.AnyAsync())
        {
            db.Produtos.AddRange(
                new Produto { Nome = "Shampoo para pets", Categoria = "Higiene", Descricao = "Exemplo acadêmico", Preco = 25.90m, QuantidadeEstoque = 12 },
                new Produto { Nome = "Coleira ajustável", Categoria = "Acessórios", Descricao = "Exemplo acadêmico", Preco = 39.90m, QuantidadeEstoque = 8 },
                new Produto { Nome = "Brinquedo mordedor", Categoria = "Brinquedos", Descricao = "Exemplo acadêmico", Preco = 19.90m, QuantidadeEstoque = 15 });
            await db.SaveChangesAsync();
        }
    }
}
