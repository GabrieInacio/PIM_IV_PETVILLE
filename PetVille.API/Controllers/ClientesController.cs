
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetVille.API.Data;
using PetVille.API.DTOs;
using PetVille.API.Models;

namespace PetVille.API.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly PetVilleContext _context;
    private readonly IPasswordHasher<Cliente> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly CloudConfigStore _cloudConfig;

    public ClientesController(
        PetVilleContext context,
        IPasswordHasher<Cliente> passwordHasher,
        IConfiguration configuration,
        CloudConfigStore cloudConfig)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _cloudConfig = cloudConfig;
    }

    // CADASTRO DE CLIENTE
    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastrar(
        CadastroClienteDto dados)
    {
        if (string.IsNullOrWhiteSpace(dados.Nome))
            return BadRequest(new { mensagem = "Informe o nome." });
        string email = dados.Email.Trim().ToLowerInvariant();

        bool emailExiste = await _context.Clientes
            .AnyAsync(c => c.Email.ToLower() == email);

        if (emailExiste)
        {
            return Conflict(new
            {
                mensagem = "Este email ja esta cadastrado."
            });
        }

        var cliente = new Cliente
        {
            Nome = dados.Nome.Trim(),
            Email = email,
            Telefone = dados.Telefone.Trim(),
            DataCadastro = DateTime.Now
        };

        cliente.SenhaHash = _passwordHasher.HashPassword(
            cliente,
            dados.Senha
        );

        _context.Clientes.Add(cliente);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Cliente cadastrado com sucesso!",
            clienteId = cliente.Id
        });
    }

    // LOGIN DE CLIENTE COM JWT
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginClienteDto dados)
    {
        string email = dados.Email.Trim().ToLowerInvariant();

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email == email);

        if (cliente == null)
        {
            return Unauthorized(new
            {
                mensagem = "Email ou senha incorretos."
            });
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            cliente,
            cliente.SenhaHash,
            dados.Senha
        );

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensagem = "Email ou senha incorretos."
            });
        }

        // Informacoes armazenadas dentro do token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, cliente.Email),
            new Claim(JwtRegisteredClaimNames.Name, cliente.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("pv_db", _cloudConfig.DatabaseIdentity)
        };

        // Recupera a chave JWT do User Secrets
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chave JWT nao encontrada."
            );

        var chaveBytes = Convert.FromBase64String(jwtKey);

        var chave = new SymmetricSecurityKey(chaveBytes);

        var credenciais = new SigningCredentials(
            chave,
            SecurityAlgorithms.HmacSha256
        );

        // Token valido por duas horas
        var expiracao = DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiracao,
            signingCredentials: credenciais
        );

        string tokenGerado = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Ok(new
        {
            mensagem = "Login realizado com sucesso!",
            clienteId = cliente.Id,
            nome = cliente.Nome,
            email = cliente.Email,
            token = tokenGerado,
            dataExpiracao = expiracao
        });
    }

    // PERFIL DO CLIENTE AUTENTICADO
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> MeuPerfil()
    {
        var clienteIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier
        )?.Value;

        if (!int.TryParse(clienteIdClaim, out int clienteId))
        {
            return Unauthorized(new
            {
                mensagem = "Token invalido."
            });
        }

        var cliente = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.Email,
                c.Telefone,
                c.DataCadastro
            })
            .FirstOrDefaultAsync();

        if (cliente == null)
        {
            return NotFound(new
            {
                mensagem = "Cliente nao encontrado."
            });
        }

        return Ok(cliente);
    }
}
