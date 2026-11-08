using System.Security.Cryptography;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetVille.API.Data;
using PetVille.API.Models;
namespace PetVille.API.Controllers;
[ApiController, Route("api/admin")]
public class AdminController(PetVilleContext db, IConfiguration config, IWebHostEnvironment environment) : ControllerBase
{
    bool Permitido()
    {
        // Somente demonstração: admin sem senha para acesso direto no próprio PC.
        // Em outros dispositivos e em produção, a chave de administrador continua obrigatória.
        if (environment.IsDevelopment() && HttpContext.Connection.RemoteIpAddress is IPAddress ip
            && IPAddress.IsLoopback(ip)) return true;
        var key = config["Admin:Key"];
        var provided = Request.Headers["X-Admin-Key"].ToString();
        return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(provided)
            && key.Length == provided.Length && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(provided));
    }
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo() => !Permitido() ? Unauthorized() : Ok(new {
        clientes = await db.Clientes.CountAsync(), pets = await db.Pets.CountAsync(),
        agendamentos = await db.Agendamentos.CountAsync(), produtos = await db.Produtos.CountAsync() });
    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes() => !Permitido() ? Unauthorized() : Ok(await db.Clientes
        .AsNoTracking().Select(c => new { c.Id, c.Nome, c.Email, c.Telefone }).ToListAsync());
    [HttpGet("agendamentos")]
    public async Task<IActionResult> Agendamentos() => !Permitido() ? Unauthorized() : Ok(await db.Agendamentos
        .AsNoTracking().OrderByDescending(a => a.DataHora).Select(a => new { a.Id, a.DataHora, a.Status,
            Cliente = a.Cliente!.Nome, Pet = a.Pet!.Nome, Servico = a.Servico!.Nome }).ToListAsync());
    public record StatusDto(string Status);
    [HttpPut("agendamentos/{id:int}/status")]
    public async Task<IActionResult> Status(int id, StatusDto dados)
    {
        if (!Permitido()) return Unauthorized();
        if (!new[] { "Pendente", "Confirmado", "Concluido", "Cancelado" }.Contains(dados.Status))
            return BadRequest(new { mensagem = "Status inválido." });
        var a = await db.Agendamentos.FindAsync(id);
        if (a is null) return NotFound();
        a.Status = dados.Status; await db.SaveChangesAsync(); return NoContent();
    }
    [HttpGet("produtos")]
    public async Task<IActionResult> Produtos() => !Permitido() ? Unauthorized() : Ok(await db.Produtos.AsNoTracking().ToListAsync());
    public record ProdutoDto(string Nome, string Categoria, string? Descricao, decimal Preco, int QuantidadeEstoque);
    [HttpPost("produtos")]
    public async Task<IActionResult> CriarProduto(ProdutoDto dados)
    {
        if (!Permitido()) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(dados.Categoria)
            || dados.Preco < 0 || dados.QuantidadeEstoque < 0) return BadRequest(new { mensagem = "Dados inválidos." });
        var p = new Produto { Nome = dados.Nome.Trim(), Categoria = dados.Categoria.Trim(),
            Descricao = dados.Descricao?.Trim() ?? "", Preco = dados.Preco, QuantidadeEstoque = dados.QuantidadeEstoque };
        db.Produtos.Add(p); await db.SaveChangesAsync(); return Ok(p);
    }
    [HttpDelete("produtos/{id:int}")]
    public async Task<IActionResult> RemoverProduto(int id)
    {
        if (!Permitido()) return Unauthorized();
        var p = await db.Produtos.FindAsync(id);
        if (p is null) return NotFound();
        db.Produtos.Remove(p); await db.SaveChangesAsync(); return NoContent();
    }
}
