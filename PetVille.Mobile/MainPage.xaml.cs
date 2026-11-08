using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace PetVille.Mobile;
public partial class MainPage : ContentPage
{
#if ANDROID
    // Emulador: 10.0.2.2. Samsung físico + adb reverse: 127.0.0.1.
    static string Api => DeviceInfo.Current.DeviceType == DeviceType.Virtual
        ? "http://10.0.2.2:5041"
        : "http://127.0.0.1:5041";
#else
    const string Api = "http://localhost:5041";
#endif
    HttpClient http = CriarClienteHttp(ObterEnderecoInicial());

    static string ObterEnderecoInicial()
    {
        var salvo = Preferences.Get("petville_api_url", Api);
#if ANDROID
        if (DeviceInfo.Current.DeviceType == DeviceType.Physical &&
            salvo.StartsWith("http://10.0.2.2", StringComparison.OrdinalIgnoreCase))
        {
            salvo = "http://127.0.0.1:5041";
            Preferences.Set("petville_api_url", salvo);
        }
#endif
        return salvo;
    }

    static HttpClient CriarClienteHttp(string? endereco)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            uri = new Uri(Api);

        return new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(25) };
    }
    bool cadastro;
    string? token;
    List<PetDto> pets = new();
    List<ServicoDto> servicos = new();
    public MainPage()
    {
        InitializeComponent();
        EnderecoApi.Text = http.BaseAddress?.ToString().TrimEnd('/');
        DataAgendamento.MinimumDate = DateTime.Today;
        DataAgendamento.MaximumDate = DateTime.Today.AddDays(90);
        // Evita que a primeira data apresentada seja um sábado ou domingo.
        var primeiroDiaUtil = DateTime.Today;
        while (primeiroDiaUtil.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            primeiroDiaUtil = primeiroDiaUtil.AddDays(1);
        DataAgendamento.Date = primeiroDiaUtil;
        DataAgendamento.DateSelected += (_, _) => AtualizarHorarios();
        ServicoPicker.SelectedIndexChanged += (_, _) => AtualizarHorarios();
        AtualizarHorarios();
        token = Preferences.Get("token", null);
        if (!string.IsNullOrEmpty(token)) _ = Abrir();
    }
    void Alternar(object? sender, EventArgs e)
    {
        cadastro = !cadastro;
        GrupoNome.IsVisible = GrupoTelefone.IsVisible = cadastro;
        TituloLogin.Text = cadastro ? "Criar minha conta" : "Entre na sua conta";
        BotaoLogin.Text = cadastro ? "Cadastrar minha conta" : "Entrar na minha conta";
        BotaoAlternar.Text = cadastro ? "Já tenho conta? Entrar" : "Não tem conta? Cadastre-se";
    }
    async void EntrarOuCadastrar(object? sender, EventArgs e)
    {
        BotaoLogin.IsEnabled = false;
        try
        {
            if (cadastro)
            {
                var r = await http.PostAsJsonAsync("/api/clientes/cadastro", new {
                    nome = Nome.Text?.Trim(), telefone = Telefone.Text?.Trim() ?? "",
                    email = Email.Text?.Trim(), senha = Senha.Text });
                if (!r.IsSuccessStatusCode) { await Erro(r); return; }
                await DisplayAlertAsync("PetVille", "Conta criada! Faça seu login.", "OK");
                Alternar(null, EventArgs.Empty);
            }
            else
            {
                var r = await http.PostAsJsonAsync("/api/clientes/login", new {
                    email = Email.Text?.Trim(), senha = Senha.Text });
                if (!r.IsSuccessStatusCode) { await Erro(r); return; }
                using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
                token = doc.RootElement.GetProperty("token").GetString();
                Preferences.Set("token", token);
                await Abrir();
            }
        }
        catch (Exception ex)
        {
            var detalhe = ex.InnerException?.Message ?? ex.Message;
            await DisplayAlertAsync("Falha de conexão",
                $"Servidor: {http.BaseAddress}\nTipo: {ex.GetType().Name}\nMotivo: {detalhe}\n" +
                "Confira o USB, a API e o comando adb reverse.", "OK");
        }
        finally { BotaoLogin.IsEnabled = true; }
    }
    async Task Abrir()
    {
        try
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var r = await http.GetAsync("/api/clientes/me");
            if (!r.IsSuccessStatusCode) { Sair(null, EventArgs.Empty); return; }
            var perfil = await r.Content.ReadFromJsonAsync<PerfilDto>();
            Saudacao.Text = $"Olá, {perfil?.Nome}!";
            AreaLogin.IsVisible = false;
            AreaInterna.IsVisible = true;
            await Carregar();
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", "Não foi possível abrir sua conta: " + ex.Message, "OK"); }
    }
    async Task Carregar()
    {
        pets = await http.GetFromJsonAsync<List<PetDto>>("/api/pets") ?? new();
        servicos = await http.GetFromJsonAsync<List<ServicoDto>>("/api/servicos") ?? new();
        PetPicker.ItemsSource = pets;
        ServicoPicker.ItemsSource = servicos;
        AtualizarHorarios();
        ListaPets.Children.Clear();
        foreach (var p in pets)
            ListaPets.Children.Add(new Label
            {
                Text = $"🐾  {p.Nome}  ·  {p.Especie} ({p.Raca})",
                TextColor = Color.FromArgb("#18332B"),
                BackgroundColor = Color.FromArgb("#EDF6F0"),
                FontSize = 14,
                Padding = new Thickness(12, 12)
            });
        if (pets.Count == 0)
            ListaPets.Children.Add(new Label
            {
                Text = "Você ainda não cadastrou um pet.",
                TextColor = Color.FromArgb("#62776D"), FontSize = 13
            });
        ListaAgendamentos.Children.Clear();
        var agendas = await http.GetFromJsonAsync<List<AgendamentoDto>>("/api/agendamentos") ?? new();
        foreach (var a in agendas)
            ListaAgendamentos.Children.Add(new Label
            {
                Text = $"📅  {a.DataHora:dd/MM/yyyy} às {a.DataHora:HH:mm}\n🐾  {a.Pet} · {a.Servico}\nStatus: {a.Status}",
                TextColor = Color.FromArgb("#18332B"),
                BackgroundColor = Color.FromArgb("#EDF6F0"),
                FontSize = 14,
                Padding = new Thickness(12, 12)
            });
        if (agendas.Count == 0)
            ListaAgendamentos.Children.Add(new Label
            {
                Text = "Você ainda não tem agendamentos.",
                TextColor = Color.FromArgb("#62776D"), FontSize = 13
            });
    }
    async void SalvarPet(object? sender, EventArgs e)
    {
        try
        {
            var r = await http.PostAsJsonAsync("/api/pets", new {
                nome = PetNome.Text?.Trim(), especie = PetEspecie.Text?.Trim(),
                raca = PetRaca.Text?.Trim() ?? "", sexo = "", dataNascimento = (DateTime?)null });
            if (!r.IsSuccessStatusCode) { await Erro(r); return; }
            PetNome.Text = PetEspecie.Text = PetRaca.Text = "";
            await Carregar();
            await DisplayAlertAsync("PetVille", "Pet cadastrado!", "OK");
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message, "OK"); }
    }
    async void Agendar(object? sender, EventArgs e)
    {
        if (PetPicker.SelectedItem is not PetDto pet || ServicoPicker.SelectedItem is not ServicoDto servico)
        { await DisplayAlertAsync("Atenção", "Selecione o pet e o serviço.", "OK"); return; }
        try
        {
            // No .NET MAUI 10, DatePicker.Date passou a ser DateTime?.
            // A data deve ser selecionada antes de agendar.
            if (DataAgendamento.Date is not DateTime dia)
            {
                await DisplayAlertAsync("Data obrigatória", "Selecione o dia do atendimento.", "OK");
                return;
            }
            dia = dia.Date;
            if (dia.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                await DisplayAlertAsync("Horário de atendimento", "Agendamos somente de segunda a sexta-feira.", "OK");
                return;
            }
            if (HorarioPicker.SelectedItem is not string horario)
            {
                await DisplayAlertAsync("Horário de atendimento", "Escolha um horário disponível entre 8h e 18h.", "OK");
                return;
            }
            var partes = horario.Split(':');
            var data = DateTime.SpecifyKind(
                dia.AddHours(int.Parse(partes[0])).AddMinutes(int.Parse(partes[1])),
                DateTimeKind.Unspecified);
            if (data <= DateTime.Now || data.AddMinutes(servico.DuracaoMinutos) > dia.AddHours(18))
            {
                await DisplayAlertAsync("Horário de atendimento", "O horário deve ser futuro e o serviço precisa terminar até as 18h.", "OK");
                return;
            }
            var r = await http.PostAsJsonAsync("/api/agendamentos", new {
                petId = pet.Id, servicoId = servico.Id, dataHora = data,
                observacao = Observacao.Text?.Trim() ?? "" });
            if (!r.IsSuccessStatusCode) { await Erro(r); return; }
            await Carregar();
            await DisplayAlertAsync("PetVille", "Agendamento solicitado!", "OK");
        }
        catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message, "OK"); }
    }
    void AtualizarHorarios()
    {
        var selecionado = HorarioPicker.SelectedItem as string;
        var horarios = new List<string>();
        if (DataAgendamento.Date is DateTime dia &&
            dia.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
        {
            var duracao = (ServicoPicker.SelectedItem as ServicoDto)?.DuracaoMinutos ?? 60;
            for (var minuto = 8 * 60; minuto + duracao <= 18 * 60; minuto += 30)
            {
                var horario = dia.Date.AddMinutes(minuto);
                if (horario > DateTime.Now) horarios.Add(horario.ToString("HH:mm"));
            }
        }
        HorarioPicker.ItemsSource = horarios;
        // Limpa um horário anterior caso o cliente mude o dia/serviço.
        HorarioPicker.SelectedItem = selecionado is not null && horarios.Contains(selecionado)
            ? selecionado : null;
    }
    void AlternarConfiguracao(object? sender, EventArgs e)
    {
        AreaServidor.IsVisible = !AreaServidor.IsVisible;
    }

    async void SalvarConfiguracao(object? sender, EventArgs e)
    {
        var endereco = EnderecoApi.Text?.Trim().TrimEnd('/');
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await DisplayAlertAsync("Endereço inválido", "Informe uma URL começando com http:// ou https://.", "OK");
            return;
        }
        // Troca o cliente HTTP sem recompilar o app. O token pertence ao servidor anterior.
        Preferences.Set("petville_api_url", endereco!);
        Preferences.Remove("token");
        token = null;
        http.Dispose();
        http = CriarClienteHttp(endereco);
        AreaInterna.IsVisible = false;
        AreaLogin.IsVisible = true;
        AreaServidor.IsVisible = false;
        ResultadoConexao.Text = "Servidor selecionado: " + endereco + ". Faça o teste abaixo.";
        await DisplayAlertAsync("Servidor atualizado", "O endereço foi salvo neste celular. Toque em testar comunicação e depois entre novamente.", "OK");
    }

    async void TestarConexao(object? sender, EventArgs e)
    {
        var endereco = EnderecoApi.Text?.Trim().TrimEnd('/');
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            ResultadoConexao.Text = "Informe primeiro uma URL válida, como http://127.0.0.1:5041";
            return;
        }
        ResultadoConexao.Text = "Testando " + endereco + "/api/health ...";
        try
        {
            // Cria um HttpClient separado para testar o campo antes de salvar.
            using var teste = CriarClienteHttp(endereco);
            using var resposta = await teste.GetAsync("/api/health");
            var corpo = await resposta.Content.ReadAsStringAsync();
            if (resposta.IsSuccessStatusCode)
            {
                ResultadoConexao.Text = "API disponível! HTTP " + (int)resposta.StatusCode + " — " + corpo;
                await DisplayAlertAsync("Conexão funcionando", "O aplicativo consegue acessar a API. Salve o endereço e faça login.", "OK");
            }
            else
            {
                ResultadoConexao.Text = $"O servidor respondeu HTTP {(int)resposta.StatusCode}: {corpo}";
            }
        }
        catch (Exception ex)
        {
            var detalhe = ex.InnerException?.Message ?? ex.Message;
            ResultadoConexao.Text = $"Falha: {ex.GetType().Name} — {detalhe}";
            await DisplayAlertAsync("Falha de conexão", ResultadoConexao.Text, "OK");
        }
    }

    async void Atualizar(object? sender, EventArgs e)
    { try { await Carregar(); } catch (Exception ex) { await DisplayAlertAsync("Erro", ex.Message, "OK"); } }
    void Sair(object? sender, EventArgs e)
    {
        Preferences.Remove("token"); token = null;
        http.DefaultRequestHeaders.Authorization = null;
        AreaInterna.IsVisible = false; AreaLogin.IsVisible = true;
    }
    async Task Erro(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        await DisplayAlertAsync("PetVille", $"Erro {(int)r.StatusCode}: {raw}", "OK");
    }
    sealed class PerfilDto { public string Nome { get; set; } = ""; }
    sealed class PetDto { public int Id { get; set; } public string Nome { get; set; } = ""; public string Especie { get; set; } = ""; public string Raca { get; set; } = ""; }
    sealed class ServicoDto { public int Id { get; set; } public string Nome { get; set; } = ""; public int DuracaoMinutos { get; set; } = 60; }
    sealed class AgendamentoDto { public DateTime DataHora { get; set; } public string Pet { get; set; } = ""; public string Servico { get; set; } = ""; public string Status { get; set; } = ""; }
}
