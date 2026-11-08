using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using MySqlConnector;

namespace PetVille.API.Data;

public sealed class TiDbProfile
{
    public string Host { get; set; } = "";
    public uint Port { get; set; } = 4000;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string Database { get; set; } = "petville";
    public string? CaCertificateFile { get; set; }
}

public sealed class CloudConfigStore
{
    private sealed class SavedSettings
    {
        public bool UseCloud { get; set; }
        public TiDbProfile? Cloud { get; set; }
    }

    private readonly object _sync = new();
    private readonly IDataProtector _protector;
    private readonly ILogger<CloudConfigStore> _logger;
    private readonly string _filePath;
    private readonly string _localConnection;
    private SavedSettings _settings = new();

    public CloudConfigStore(IConfiguration config, IDataProtectionProvider dataProtection, ILogger<CloudConfigStore> logger)
    {
        _logger = logger;
        _protector = dataProtection.CreateProtector("PetVille.TiDB.CloudCredentials.v1");
        _localConnection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configuração do MySQL local não encontrada.");
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PetVilleFaculdade");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "tidb-connection.protected");
        if (File.Exists(_filePath))
        {
            try
            {
                var json = _protector.Unprotect(File.ReadAllText(_filePath));
                _settings = JsonSerializer.Deserialize<SavedSettings>(json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Não foi possível carregar a configuração protegida do TiDB: {Type}", ex.GetType().Name);
            }
        }
    }

    public bool IsCloudActive { get { lock (_sync) return _settings.UseCloud && _settings.Cloud is not null; } }
    public bool HasCloud { get { lock (_sync) return _settings.Cloud is not null; } }
    public string? CloudHost { get { lock (_sync) return _settings.Cloud?.Host; } }
    public string Mode => IsCloudActive ? "TiDB Cloud" : "MySQL local";
    // Separa as sessões JWT de contas que possam ter IDs iguais em bancos distintos.
    public string DatabaseIdentity
    {
        get
        {
            lock (_sync)
            {
                if (_settings.UseCloud && _settings.Cloud is not null)
                    return "tidb:" + _settings.Cloud.Host.ToLowerInvariant() + "/" + _settings.Cloud.Database.ToLowerInvariant();
                return "local:petville";
            }
        }
    }
    public string ActiveConnectionString
    {
        get
        {
            lock (_sync)
            {
                return _settings.UseCloud && _settings.Cloud is not null
                    ? BuildConnectionString(_settings.Cloud)
                    : _localConnection;
            }
        }
    }

    public string LocalConnectionString => _localConnection;

    public void ActivateCloud(TiDbProfile profile)
    {
        lock (_sync)
        {
            Save(new SavedSettings { Cloud = profile, UseCloud = true });
        }
    }

    public bool UseSavedCloud()
    {
        lock (_sync)
        {
            if (_settings.Cloud is null) return false;
            Save(new SavedSettings { Cloud = _settings.Cloud, UseCloud = true });
            return true;
        }
    }

    public void UseLocal()
    {
        lock (_sync)
        {
            Save(new SavedSettings { Cloud = _settings.Cloud, UseCloud = false });
        }
    }

    private void Save(SavedSettings next)
    {
        var protectedText = _protector.Protect(JsonSerializer.Serialize(next));
        var temp = _filePath + ".tmp";
        File.WriteAllText(temp, protectedText);
        File.Move(temp, _filePath, true);
        _settings = next;
    }

    public static string BuildConnectionString(TiDbProfile p)
    {
        // VerifyFull valida a cadeia de certificados e o nome do host.
        var cb = new MySqlConnectionStringBuilder
        {
            Server = p.Host.Trim(),
            Port = p.Port,
            UserID = p.User.Trim(),
            Password = p.Password,
            Database = p.Database.Trim(),
            SslMode = MySqlSslMode.VerifyFull,
            ConnectionTimeout = 15,
            DefaultCommandTimeout = 30,
            AllowPublicKeyRetrieval = false
        };
        if (!string.IsNullOrWhiteSpace(p.CaCertificateFile))
            cb.SslCa = p.CaCertificateFile;
        return cb.ConnectionString;
    }
}
