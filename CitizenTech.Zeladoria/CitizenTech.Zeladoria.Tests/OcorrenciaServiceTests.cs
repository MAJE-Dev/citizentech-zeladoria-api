using Microsoft.EntityFrameworkCore;
using Xunit;
using CitizenTech.Zeladoria.Data;
using CitizenTech.Zeladoria.Services;
using CitizenTech.Zeladoria.Models;

public class OcorrenciaServiceTests
{
    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarOcorrencia_QuandoIdExistir()
    {
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Db_Test_ObterPorId")
            .Options;

        using var context = new AppDbContext(options);

        // Adiciona dados auxiliares ou garante que as tabelas relacionadas têm registos para o Include funcionar
        context.Categorias.Add(new Categoria { Id = 1, Nome = "Buraco na Via" });
        context.StatusOcorrencias.Add(new StatusOcorrencia { Id = 1, DescricaoStatus = "Aberta" });
        context.Usuarios.Add(new Usuario { Id = 1, Nome = "Cidadão Teste", Email = "teste@email.com" });

        // Adiciona a ocorrência usando o novo modelo relacional
        context.Ocorrencias.Add(new Ocorrencia
        {
            Id = 10,
            Titulo = "Buraco na rua",
            Descricao = "Buraco grande na via pública",
            CategoriaId = 1,          
            Endereco = "Rua Exemplo, 123",
            StatusId = 1,             
            UsuarioId = 1,           
            DataRegistro = DateTime.UtcNow 
        });
        await context.SaveChangesAsync();

        var service = new OcorrenciaService(context);

        
        var resultado = await service.ObterPorIdAsync(10);

       
        Assert.NotNull(resultado);
        Assert.Equal(10, resultado.Id);
        Assert.Equal("Buraco na rua", resultado.Titulo);
        Assert.Equal("Aberta", resultado.StatusDescricao);
    }
}