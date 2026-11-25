using System.Text;
using CitizenTech.Zeladoria.Api;
using Microsoft.Data.SqlClient;

namespace CitizenTech.Zeladoria.Tests;

/// <summary>Testes unitários das regras de validação (complementam os testes T01–T10 executados contra a API real).</summary>
public class ValidacoesTests
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];

    [Theory]
    [InlineData("foto.png", "image/png", true)]
    [InlineData("foto.PNG", "image/png", true)]
    [InlineData("foto.png", "image/jpeg", false)] // MIME não bate com a assinatura
    [InlineData("foto.jpg", "image/png", false)]  // extensão não bate
    public void Png_exige_extensao_mime_e_assinatura_coerentes(string nome, string mime, bool esperado) =>
        Assert.Equal(esperado, ValidadorImagem.FormatoCoerente(nome, mime, Png, out _));

    [Theory]
    [InlineData("foto.jpg")]
    [InlineData("foto.jpeg")]
    public void Jpeg_valido_e_aceito(string nome)
    {
        Assert.True(ValidadorImagem.FormatoCoerente(nome, "image/jpeg", Jpeg, out var mime));
        Assert.Equal("image/jpeg", mime);
    }

    [Fact]
    public void Png_disfarcado_com_texto_e_rejeitado() =>
        Assert.False(ValidadorImagem.FormatoCoerente("falso.png", "image/png", Encoding.UTF8.GetBytes("isto nao e imagem"), out _));

    [Fact]
    public void Nome_com_caminho_do_cliente_vira_apenas_o_arquivo() =>
        Assert.Equal("foto.png", ValidadorImagem.NomeSeguro(@"C:\Users\x\Desktop\foto.png"));

    [Fact]
    public void Equipe_valida_executor_e_lider()
    {
        var equipe = new Equipe("Teste", "3ESPH", "000001",
        [
            new Integrante("Ana Exemplo", "000001", "3ESPH", "AnaExemplo_RM000001", true),
            new Integrante("Beto Exemplo", "000002", "3ESPH", "BetoExemplo_RM000002", false)
        ]);
        equipe.Validar();
        Assert.True(equipe.Contem("BetoExemplo_RM000002"));
        Assert.False(equipe.Contem("Intruso_RM999999"));
        Assert.False(equipe.Contem(""));
    }

    [Fact]
    public void Equipe_sem_lider_ou_com_identificacao_errada_falha()
    {
        var semLider = new Equipe("T", "3ESPH", "000001", [new Integrante("Ana", "000001", "3ESPH", "Ana_RM000001", false)]);
        Assert.Throws<InvalidOperationException>(semLider.Validar);
        var rmErrado = new Equipe("T", "3ESPH", "000001", [new Integrante("Ana", "000001", "3ESPH", "Ana_RM999999", true)]);
        Assert.Throws<InvalidOperationException>(rmErrado.Validar);
    }

    [Fact]
    public void Falha_de_banco_e_reconhecida_mesmo_embrulhada()
    {
        // SqlException não tem construtor público; simulamos a cadeia com TimeoutException (também tratada como indisponibilidade).
        Assert.True(TratadorDeFalhas.BancoIndisponivel(new InvalidOperationException("x", new TimeoutException())));
        Assert.False(TratadorDeFalhas.BancoIndisponivel(new InvalidOperationException("regra")));
        Assert.True(typeof(SqlException).IsAssignableTo(typeof(System.Data.Common.DbException)));
    }
}
