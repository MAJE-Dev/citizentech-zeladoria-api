using System.Text.Json.Serialization;
using CitizenTech.Zeladoria.Data;
using CitizenTech.Zeladoria.Middlewares;
using CitizenTech.Zeladoria.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configuração dos Controllers e serialização de Enums como texto
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configuração do Banco de Dados em Memória (EF Core) com suporte a transações simuladas
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ZeladoriaDb")
           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

// Injeção de Dependência da camada de Serviço
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();

// Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Garante que os dados iniciais (Seed) sejam carregados ao iniciar a API
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Middleware Global de Tratamento de Erros
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();