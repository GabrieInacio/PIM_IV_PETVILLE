
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetVille.API.Data;
using PetVille.API.Models;

namespace PetVille.API.Controllers;

[ApiController]
[Route("api/pets")]
[Authorize]
public class PetsController : ControllerBase
{
    private readonly PetVilleContext _context;

    public PetsController(PetVilleContext context)
    {
        _context = context;
    }

    // Recupera o ID do cliente autenticado
    private bool TentarObterClienteId(out int clienteId)
    {
        var id = User.FindFirst(
            ClaimTypes.NameIdentifier
        )?.Value;

        return int.TryParse(id, out clienteId);
    }

    // LISTAR PETS DO CLIENTE
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        if (!TentarObterClienteId(out int clienteId))
            return Unauthorized();

        var pets = await _context.Pets
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .ToListAsync();

        return Ok(pets);
    }

    // CADASTRAR PET
    [HttpPost]
    public async Task<IActionResult> Cadastrar(Pet dados)
    {
        if (!TentarObterClienteId(out int clienteId))
            return Unauthorized();

        var pet = new Pet
        {
            Nome = dados.Nome.Trim(),
            Especie = dados.Especie.Trim(),
            Raca = dados.Raca.Trim(),
            DataNascimento = dados.DataNascimento,
            Sexo = dados.Sexo.Trim(),
            ClienteId = clienteId
        };

        _context.Pets.Add(pet);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Listar),
            new { id = pet.Id },
            new
            {
                mensagem = "Pet cadastrado com sucesso!",
                petId = pet.Id
            }
        );
    }

    // CONSULTAR PET POR ID
    [HttpGet("{id}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        if (!TentarObterClienteId(out int clienteId))
            return Unauthorized();

        var pet = await _context.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.ClienteId == clienteId
            );

        if (pet == null)
        {
            return NotFound(new
            {
                mensagem = "Pet nao encontrado."
            });
        }

        return Ok(pet);
    }

    // ATUALIZAR PET
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        int id,
        Pet dados)
    {
        if (!TentarObterClienteId(out int clienteId))
            return Unauthorized();

        var pet = await _context.Pets
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.ClienteId == clienteId
            );

        if (pet == null)
        {
            return NotFound(new
            {
                mensagem = "Pet nao encontrado."
            });
        }

        pet.Nome = dados.Nome.Trim();
        pet.Especie = dados.Especie.Trim();
        pet.Raca = dados.Raca.Trim();
        pet.DataNascimento = dados.DataNascimento;
        pet.Sexo = dados.Sexo.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Pet atualizado com sucesso!"
        });
    }

    // EXCLUIR PET
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (!TentarObterClienteId(out int clienteId))
            return Unauthorized();

        var pet = await _context.Pets
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.ClienteId == clienteId
            );

        if (pet == null)
        {
            return NotFound(new
            {
                mensagem = "Pet nao encontrado."
            });
        }

        _context.Pets.Remove(pet);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Pet excluido com sucesso!"
        });
    }
}
