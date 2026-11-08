using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PetVille.API.Data;

namespace PetVille.API.Controllers;

[ApiController]
[Route("api/configuracao")]
public sealed class CloudSetupController(CloudConfigStore store, IWebHostEnvironment environment) : ControllerBase
{
    // Configuração apenas a partir do próprio computador, em modo Development.
    private bool AllowLocal()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;
        if (!environment.IsDevelopment() || remote is null || !IPAddress.IsLoopback(remote)) return false;
        var origin = Request.Headers["Origin"].ToString();
        if (origin.Length > 0 && (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme != Request.Scheme)) return false;
        return true;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (!AllowLocal()) return NotFound();
        bool conectado;
        try
        {
            await using var db = CreateDb(store.ActiveConnectionString);
            conectado = await db.Database.CanConnectAsync();
        }
        catch { conectado = false; }
        return Ok(new
        {
            modo = store.Mode,
            online = conectado,
            nuvemConfigurada = store.HasCloud,
            servidor = store.CloudHost,
            observacao = "A API continua rodando neste computador; apenas o banco pode estar na nuvem."
        });
    }

    public sealed record CloudRequest(string Host, int Port, string User, string Password, string Database, string? CaCertificateFile);

    [HttpPost("tidb")]
    public async Task<IActionResult> Connect(CloudRequest input)
    {
        if (!AllowLocal()) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Host) || !Regex.IsMatch(input.Host.Trim(), @"^[a-zA-Z0-9.-]{4,255}$")
            || input.Host.Contains("..", StringComparison.Ordinal) || input.Host.StartsWith('-')
            || string.IsNullOrWhiteSpace(input.User) || string.IsNullOrWhiteSpace(input.Password)
            || input.Port is < 1 or > 65535 || !Regex.IsMatch(input.Database ?? "", @"^[a-zA-Z][a-zA-Z0-9_]{0,40}$"))
            return BadRequest(new { mensagem = "Confira host, porta, usuário, senha e nome do banco (letras, números e _)." });

        var profile = new TiDbProfile
        {
            Host = input.Host.Trim(), Port = (uint)input.Port, User = input.User.Trim(),
            Password = input.Password, Database = input.Database.Trim(),
            CaCertificateFile = string.IsNullOrWhiteSpace(input.CaCertificateFile) ? null : input.CaCertificateFile.Trim()
        };
        try
        {
            // Cria o banco se o usuário TiDB tiver permissão.
            var connectionWithoutDb = new MySqlConnectionStringBuilder(CloudConfigStore.BuildConnectionString(profile)) { Database = "" };
            await using (var conn = new MySqlConnection(connectionWithoutDb.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{profile.Database}` CHARACTER SET utf8mb4";
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cloudDb = CreateDb(CloudConfigStore.BuildConnectionString(profile)))
            {
                await DatabaseBootstrap.InitializeAsync(cloudDb);
            }
            store.ActivateCloud(profile);
            return Ok(new { mensagem = "Conexão com TiDB Cloud configurada. Tabelas verificadas e PetVille usando a nuvem.", modo = store.Mode });
        }
        catch (Exception ex)
        {
            // Não expõe a senha nem altera a conexão ativa se houver falha.
            var msg = ex.Message.Replace(profile.Password, "[oculto]", StringComparison.Ordinal);
            if (msg.Length > 550) msg = msg[..550];
            return BadRequest(new { mensagem = "Não foi possível conectar/criar tabelas na nuvem. " + msg,
                dica = "Confira as credenciais em Connect, o plano MySQL-compatible e o certificado TLS." });
        }
    }

    [HttpPost("local")]
    public async Task<IActionResult> SwitchLocal()
    {
        if (!AllowLocal()) return NotFound();
        try
        {
            await using var localDb = CreateDb(store.LocalConnectionString);
            await DatabaseBootstrap.InitializeAsync(localDb);
            store.UseLocal();
            return Ok(new { mensagem = "PetVille voltou ao MySQL local. Os dados da nuvem foram preservados." });
        }
        catch { return BadRequest(new { mensagem = "O MySQL local não está acessível. A configuração atual foi mantida." }); }
    }

    [HttpPost("usar-nuvem-salva")]
    public async Task<IActionResult> SwitchCloud()
    {
        if (!AllowLocal()) return NotFound();
        if (!store.HasCloud) return BadRequest(new { mensagem = "Ainda não existe conexão TiDB configurada." });
        // Alterna apenas após testar conexão. Em caso de falha, volta ao banco anterior.
        var before = store.IsCloudActive;
        try
        {
            if (!store.UseSavedCloud()) return BadRequest(new { mensagem = "Nuvem não configurada." });
            await using var cloudDb = CreateDb(store.ActiveConnectionString);
            await DatabaseBootstrap.InitializeAsync(cloudDb);
            return Ok(new { mensagem = "PetVille está usando o TiDB Cloud novamente." });
        }
        catch (Exception)
        {
            if (!before) store.UseLocal();
            return BadRequest(new { mensagem = "Não foi possível acessar o TiDB Cloud. O banco anterior foi mantido." });
        }
    }

    private static PetVilleContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PetVilleContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))).Options;
        return new PetVilleContext(options);
    }
}
