using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetVille.API.Data;
using PetVille.API.Models;
namespace PetVille.API.Controllers;

[ApiController, Route("api/agendamentos"), Authorize]
public class AgendamentosController(PetVilleContext db) : ControllerBase
{
    int ClienteId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public record NovoAgendamento(int PetId, int ServicoId, DateTime DataHora, string? Observacao);

    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await db.Agendamentos.AsNoTracking()
        .Where(a => a.ClienteId == ClienteId).OrderByDescending(a => a.DataHora)
        .Select(a => new { a.Id, a.DataHora, a.Status, a.Observacao, a.PetId, Pet = a.Pet!.Nome,
            a.ServicoId, Servico = a.Servico!.Nome }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Criar(NovoAgendamento dados)
    {
        if (!await db.Pets.AnyAsync(p => p.Id == dados.PetId && p.ClienteId == ClienteId))
            return BadRequest(new { mensagem = "Pet inválido." });

        var servico = await db.Servicos.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dados.ServicoId && s.Ativo);
        if (servico is null) return BadRequest(new { mensagem = "Serviço inválido." });

        // Enforce opening days / times on the server, including calls made outside the website.
        var horarioLocal = HorarioAtendimento.ConverterParaSaoPaulo(dados.DataHora);
        var erro = HorarioAtendimento.Validar(horarioLocal, servico.DuracaoMinutos);
        if (erro is not null) return BadRequest(new { mensagem = erro });

        // A single pet must not be booked for two overlapping services.
        var inicioDia = horarioLocal.Date;
        var fimDia = inicioDia.AddDays(1);
        var ocupados = await db.Agendamentos.AsNoTracking()
            .Include(a => a.Servico)
            .Where(a => a.PetId == dados.PetId && a.Status != "Cancelado"
                && a.DataHora >= inicioDia && a.DataHora < fimDia)
            .ToListAsync();
        if (ocupados.Any(a => a.DataHora < horarioLocal.AddMinutes(servico.DuracaoMinutos)
                && horarioLocal < a.DataHora.AddMinutes(a.Servico?.DuracaoMinutos ?? 60)))
            return Conflict(new { mensagem = "Esse pet já possui um atendimento nesse intervalo. Escolha outro horário." });

        var agendamento = new Agendamento
        {
            ClienteId = ClienteId,
            PetId = dados.PetId,
            ServicoId = dados.ServicoId,
            DataHora = horarioLocal,
            Observacao = dados.Observacao?.Trim() ?? "",
            Status = "Pendente"
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Agendamento solicitado!", agendamento.Id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var agendamento = await db.Agendamentos.FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == ClienteId);
        if (agendamento is null) return NotFound();
        agendamento.Status = "Cancelado";
        await db.SaveChangesAsync();
        return NoContent();
    }
}
