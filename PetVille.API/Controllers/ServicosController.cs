
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetVille.API.Data;
using PetVille.API.Models;

namespace PetVille.API.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
public class ServicosController : ControllerBase
{
    private readonly PetVilleContext _context;

    public ServicosController(PetVilleContext context)
    {
        _context = context;
    }

    private bool Administrador()
    {
        var esperado = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Admin:Key"];
        var recebido = Request.Headers["X-Admin-Key"].ToString();
        return !string.IsNullOrWhiteSpace(esperado) && recebido.Length == esperado.Length &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
    }

    // LISTAR TODOS OS SERVICOS
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var servicos = await _context.Servicos
            .AsNoTracking()
            .ToListAsync();

        return Ok(servicos);
    }

    // CONSULTAR SERVICO POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var servico = await _context.Servicos
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (servico == null)
        {
            return NotFound(new
            {
                mensagem = "Servico nao encontrado."
            });
        }

        return Ok(servico);
    }

    // CADASTRAR SERVICO
    [HttpPost]
    public async Task<IActionResult> Cadastrar(Servico dados)
    {
        if (!Administrador()) return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });
        var servico = new Servico
        {
            Nome = dados.Nome.Trim(),
            Descricao = dados.Descricao.Trim(),
            Preco = dados.Preco,
            DuracaoMinutos = dados.DuracaoMinutos,
            Ativo = dados.Ativo
        };

        _context.Servicos.Add(servico);

        await _context.SaveChangesAsync();

        return Created(
            $"/api/servicos/{servico.Id}",
            new
            {
                mensagem = "Servico cadastrado com sucesso!",
                servicoId = servico.Id
            }
        );
    }

    // ATUALIZAR SERVICO
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        int id,
        Servico dados)
    {
        if (!Administrador()) return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });
        var servico = await _context.Servicos
            .FirstOrDefaultAsync(s => s.Id == id);

        if (servico == null)
        {
            return NotFound(new
            {
                mensagem = "Servico nao encontrado."
            });
        }

        servico.Nome = dados.Nome.Trim();
        servico.Descricao = dados.Descricao.Trim();
        servico.Preco = dados.Preco;
        servico.DuracaoMinutos = dados.DuracaoMinutos;
        servico.Ativo = dados.Ativo;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Servico atualizado com sucesso!"
        });
    }

    // EXCLUIR SERVICO
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (!Administrador()) return Unauthorized(new { mensagem = "Acesso restrito ao administrador." });
        var servico = await _context.Servicos
            .FirstOrDefaultAsync(s => s.Id == id);

        if (servico == null)
        {
            return NotFound(new
            {
                mensagem = "Servico nao encontrado."
            });
        }

        _context.Servicos.Remove(servico);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Servico excluido com sucesso!"
        });
    }
}
