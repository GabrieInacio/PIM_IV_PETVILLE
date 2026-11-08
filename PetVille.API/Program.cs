using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetVille.API.Data;
using PetVille.API.Models;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurada.");
byte[] key;
try { key = Convert.FromBase64String(jwtKey); }
catch { throw new InvalidOperationException("Jwt:Key deve ser Base64."); }
if (key.Length < 32) throw new InvalidOperationException("Jwt:Key precisa de pelo menos 32 bytes.");

builder.Services.AddDataProtection().SetApplicationName("PetVille-Faculdade");
builder.Services.AddSingleton<CloudConfigStore>();
// Cada requisição obtém o banco ativo: local ou TiDB Cloud, sem editar código nem reiniciar.
builder.Services.AddDbContext<PetVilleContext>((sp, options) =>
    options.UseMySql(sp.GetRequiredService<CloudConfigStore>().ActiveConnectionString,
        new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = ctx =>
        {
            var cloud = ctx.HttpContext.RequestServices.GetRequiredService<CloudConfigStore>();
            var scopedDatabase = ctx.Principal?.FindFirst("pv_db")?.Value;
            if (scopedDatabase != cloud.DatabaseIdentity)
                ctx.Fail("A sessão pertence a outro banco. Faça login novamente.");
            return Task.CompletedTask;
        }
    };
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<Cliente>, PasswordHasher<Cliente>>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
var app = builder.Build();

// Em caso de falta de internet, permite abrir a página de configuração e voltar ao MySQL local.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DatabaseBootstrap.InitializeAsync(scope.ServiceProvider.GetRequiredService<PetVilleContext>());
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Banco indisponível na inicialização. Abra /nuvem.html para conferir a conexão.");
    }
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "PetVille API iniciada" }));
app.MapGet("/api/health/database", async (PetVilleContext db, CloudConfigStore store) =>
    await db.Database.CanConnectAsync()
    ? Results.Ok(new { status = "Conectado", tipo = store.Mode })
    : Results.Problem("Banco indisponível. Abra /nuvem.html."));
app.Run();
