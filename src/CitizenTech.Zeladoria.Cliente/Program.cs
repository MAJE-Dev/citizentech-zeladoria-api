using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

// Cliente HTTP de testes da Zeladoria API Urbana: consome o MESMO contrato da API (sem acesso direto ao banco).
// Uso: dotnet run --project src/CitizenTech.Zeladoria.Cliente -- <comando> [argumentos]
Console.OutputEncoding = System.Text.Encoding.UTF8;
var baseUrl = Environment.GetEnvironmentVariable("ZELADORIA_API") ?? "http://localhost:5080";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
var jsonSaida = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

if (args.Length == 0) return Ajuda();
Console.WriteLine($"[cliente] {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz} | API {baseUrl} | comando: {string.Join(' ', args)}");
try
{
    return args[0] switch
    {
        "equipe" => await Mostrar(await http.GetAsync("api/equipe")),
        "ocorrencias" => await Mostrar(await http.GetAsync("api/ocorrencias")),
        "ocorrencia" => await Mostrar(await http.GetAsync($"api/ocorrencias/{int.Parse(args[1])}")),
        "atendimentos" => await Mostrar(await http.GetAsync($"api/ocorrencias/{int.Parse(args[1])}/atendimentos")),
        "imagens" => await Mostrar(await http.GetAsync($"api/ocorrencias/{int.Parse(args[1])}/imagens")),
        "metadados" => await Mostrar(await http.GetAsync($"api/imagens/{int.Parse(args[1])}")),
        "enviar-imagem" => await EnviarImagem(int.Parse(args[1]), args[2], args[3], args[4]),
        "baixar-imagem" => await BaixarImagem(int.Parse(args[1]), args[2], args.Contains("--abrir")),
        "atender" => await Atender(int.Parse(args[1]), decimal.Parse(args[2], CultureInfo.InvariantCulture),
                                   args[3], args[4], args.Contains("--simular-falha")),
        _ => Ajuda()
    };
}
catch (HttpRequestException ex)
{
    // T10: API parada -> mensagem compreensível, sem sucesso aparente e sem queda não tratada.
    Console.WriteLine($"[ERRO DE COMUNICAÇÃO] Não foi possível falar com a API em {baseUrl}: {ex.Message}");
    Console.WriteLine("Nenhuma confirmação foi recebida. Verifique se a API está em execução e tente novamente.");
    return 2;
}
catch (TaskCanceledException)
{
    Console.WriteLine($"[TEMPO ESGOTADO] A API em {baseUrl} não respondeu em {http.Timeout.TotalSeconds}s. Nenhuma confirmação foi recebida.");
    return 2;
}
catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or FileNotFoundException)
{
    Console.WriteLine($"[ENTRADA INVÁLIDA] {ex.Message}");
    return Ajuda();
}

async Task<int> Mostrar(HttpResponseMessage resp)
{
    Console.WriteLine($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} | Content-Type: {resp.Content.Headers.ContentType}");
    var corpo = await resp.Content.ReadAsStringAsync();
    if (corpo.Length > 0)
    {
        try { Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(corpo).RootElement, jsonSaida)); }
        catch (JsonException) { Console.WriteLine(corpo); }
    }
    return resp.IsSuccessStatusCode ? 0 : 1;
}

async Task<int> EnviarImagem(int ocorrenciaId, string caminho, string titulo, string executor)
{
    await using var arquivo = File.OpenRead(caminho);                       // stream do arquivo local
    var hashLocal = Convert.ToHexString(await SHA256.HashDataAsync(arquivo)); // leitura assíncrona
    arquivo.Position = 0;
    Console.WriteLine($"Arquivo local: {Path.GetFileName(caminho)} | {arquivo.Length} bytes | SHA-256 {hashLocal}");

    using var form = new MultipartFormDataContent();
    var conteudo = new StreamContent(arquivo);                              // envia o stream, sem carregar tudo antes
    conteudo.Headers.ContentType = new MediaTypeHeaderValue(MimePorExtensao(caminho));
    form.Add(conteudo, "Arquivo", Path.GetFileName(caminho));
    form.Add(new StringContent(titulo), "Titulo");
    form.Add(new StringContent(executor), "Executor");
    return await Mostrar(await http.PostAsync($"api/ocorrencias/{ocorrenciaId}/imagens", form));
}

async Task<int> BaixarImagem(int imagemId, string destino, bool abrir)
{
    using var resp = await http.GetAsync($"api/imagens/{imagemId}/conteudo", HttpCompletionOption.ResponseHeadersRead);
    if (!resp.IsSuccessStatusCode) return await Mostrar(resp);

    await using (var corpo = await resp.Content.ReadAsStreamAsync())        // stream da resposta HTTP
    await using (var saida = File.Create(destino))                          // stream do arquivo recuperado
        await corpo.CopyToAsync(saida);

    await using var lido = File.OpenRead(destino);
    var hash = Convert.ToHexString(await SHA256.HashDataAsync(lido));
    Console.WriteLine($"HTTP {(int)resp.StatusCode} | Content-Type: {resp.Content.Headers.ContentType}");
    Console.WriteLine($"Origem informada pela API: {string.Join("", resp.Headers.TryGetValues("X-Origem-Imagem", out var o) ? o : ["?"])}");
    Console.WriteLine($"SHA-256 gravado no banco : {string.Join("", resp.Headers.TryGetValues("X-Imagem-SHA256", out var h) ? h : ["?"])}");
    Console.WriteLine($"SHA-256 do recuperado    : {hash}");
    Console.WriteLine($"Arquivo recuperado       : {Path.GetFullPath(destino)} ({lido.Length} bytes) - NÃO é o arquivo original, veio do GET /conteudo");
    if (abrir) Process.Start(new ProcessStartInfo(Path.GetFullPath(destino)) { UseShellExecute = true });
    return 0;
}

async Task<int> Atender(int ocorrenciaId, decimal quantidade, string equipeCampo, string executor, bool simularFalha) =>
    await Mostrar(await http.PostAsJsonAsync($"api/ocorrencias/{ocorrenciaId}/atendimentos",
        new { quantidade, equipeCampoCodigo = equipeCampo, executor, simularFalha }));

static string MimePorExtensao(string caminho) => Path.GetExtension(caminho).ToLowerInvariant() switch
{
    ".png" => "image/png",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".gif" => "image/gif",
    _ => "application/octet-stream"
};

static int Ajuda()
{
    Console.WriteLine("""
        Comandos (variável ZELADORIA_API muda a URL; padrão http://localhost:5080):
          equipe | ocorrencias | ocorrencia <id> | atendimentos <id> | imagens <ocorrenciaId> | metadados <imagemId>
          enviar-imagem <ocorrenciaId> <arquivo> <titulo> <executor>
          baixar-imagem <imagemId> <destino> [--abrir]
          atender <ocorrenciaId> <quantidade> <codigoEquipeCampo> <executor> [--simular-falha]
        """);
    return 1;
}
