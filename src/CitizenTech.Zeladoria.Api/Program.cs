using System.Text.Json;
using CitizenTech.Zeladoria.Api;
using CitizenTech.Zeladoria.Api.Dados;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8; // logs com acentos corretos quando redirecionados para arquivo

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ");

// Identificação acadêmica local (NÃO é autenticação): equipe.json validado na inicialização.
var equipe = JsonSerializer.Deserialize<Equipe>(
        File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "equipe.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Preencha equipe.json.");
equipe.Validar();
builder.Services.AddSingleton(equipe);

// A API aceita até 2 MiB por imagem (ValidadorImagem). O limite do multipart/servidor fica em 3 MiB
// para que um arquivo entre 2 e 3 MiB chegue à validação da API e receba 413 com mensagem clara.
const long LimiteRequisicao = 3 * 1024 * 1024;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = LimiteRequisicao);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = LimiteRequisicao);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeFalhas>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "Zeladoria API Urbana - GS 2026.2", Version = "v1" }));

// Um banco: SQL Server. A conexão vem de user-secrets/variável de ambiente, nunca do ZIP.
var conexao = builder.Configuration.GetConnectionString("Banco");
if (string.IsNullOrWhiteSpace(conexao))
    throw new InvalidOperationException("Configure ConnectionStrings:Banco via user-secrets (ver README).");
builder.Services.AddDbContext<AppDb>(o => o.UseSqlServer(conexao));

var app = builder.Build();

// Mensagens genéricas na resposta (ProblemDetails JSON); detalhes ficam só no log local.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Logger.LogInformation("Zeladoria API iniciada | CitizenTech={CitizenTech} | turma={Turma} | lider RM={Lider} | ambiente={Ambiente}",
    equipe.CitizenTech, equipe.Turma, equipe.LiderRm, app.Environment.EnvironmentName);

app.Run();
