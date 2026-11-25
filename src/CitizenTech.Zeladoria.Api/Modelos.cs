using System.Text.RegularExpressions;

namespace CitizenTech.Zeladoria.Api;

// ---------- Identificação acadêmica (equipe.json) ----------
public sealed record Integrante(string NomeCompleto, string Rm, string Turma, string Identificacao, bool Lider);

public sealed partial record Equipe(string CitizenTech, string Turma, string LiderRm, Integrante[] Integrantes)
{
    /// <summary>Regra de executor: só integrantes reais da equipe podem gravar imagens/atendimentos.</summary>
    public bool Contem(string? executor) =>
        !string.IsNullOrWhiteSpace(executor) && Integrantes.Any(x => x.Identificacao == executor);

    /// <summary>Falha na inicialização se equipe.json estiver incoerente (1 a 5 integrantes, 1 líder, NomeAluno_RM).</summary>
    public void Validar()
    {
        if (Integrantes is null || Integrantes.Length is < 1 or > 5)
            throw new InvalidOperationException("equipe.json: informe de 1 a 5 integrantes.");
        if (Integrantes.Count(x => x.Lider) != 1 || !Integrantes.Any(x => x.Lider && x.Rm == LiderRm))
            throw new InvalidOperationException("equipe.json: deve haver exatamente um líder, com RM igual a LiderRm.");
        foreach (var i in Integrantes)
        {
            if (!FormatoIdentificacao().IsMatch(i.Identificacao) || !i.Identificacao.EndsWith("_RM" + i.Rm, StringComparison.Ordinal))
                throw new InvalidOperationException($"equipe.json: identificação inválida '{i.Identificacao}' (use NomeAluno_RM + número do RM).");
            if (i.Turma != Turma)
                throw new InvalidOperationException("equipe.json: todos os integrantes devem ser da mesma turma.");
        }
    }

    [GeneratedRegex("^[A-Za-z]+_RM[0-9]+$")]
    private static partial Regex FormatoIdentificacao();
}

// ---------- Domínio (5 tabelas) ----------
public static class Situacoes
{
    public const string Aberta = "ABERTA";
    public const string EmAtendimento = "EM_ATENDIMENTO";
    public const string Concluida = "CONCLUIDA";
}

/// <summary>REGIAO: território (subprefeitura/bairro). 1 região -> 0..N ocorrências.</summary>
public sealed class Regiao
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
}

/// <summary>CATEGORIA_OCORRENCIA: tipo do problema urbano. 1 categoria -> 0..N ocorrências.</summary>
public sealed class CategoriaOcorrencia
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
}

/// <summary>OCORRENCIA: problema relatado, com quantidade de serviço pendente ("saldo").</summary>
public sealed class Ocorrencia
{
    public int Id { get; set; }
    public int RegiaoId { get; set; }
    public int CategoriaId { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Unidade { get; set; } = "";
    public decimal QuantidadeEstimada { get; set; }
    public decimal QuantidadePendente { get; set; }
    public string Situacao { get; set; } = Situacoes.Aberta;
    public DateTime AbertaUtc { get; set; }
}

/// <summary>IMAGEM_OCORRENCIA: foto do problema guardada em bytes (varbinary(max)) com metadados técnicos.</summary>
public sealed class ImagemOcorrencia
{
    public int Id { get; set; }
    public int OcorrenciaId { get; set; }
    public string Titulo { get; set; } = "";
    public string NomeArquivo { get; set; } = "";
    public string Mime { get; set; } = "";
    public int TamanhoBytes { get; set; }
    public string HashSha256 { get; set; } = "";
    public byte[] Dados { get; set; } = [];
    public string EquipeRm { get; set; } = "";
    public string Executor { get; set; } = "";
    public DateTime EnviadaUtc { get; set; }
}

/// <summary>ATENDIMENTO_OCORRENCIA: serviço executado em campo; gravado junto com a baixa do pendente.</summary>
public sealed class AtendimentoOcorrencia
{
    public int Id { get; set; }
    public int OcorrenciaId { get; set; }
    public decimal Quantidade { get; set; }
    public string EquipeCampoCodigo { get; set; } = "";
    public string EquipeRm { get; set; } = "";
    public string Executor { get; set; } = "";
    public DateTime CriadoUtc { get; set; }
}

// ---------- Contratos de entrada (validados manualmente, mensagens em português) ----------
public sealed class UploadImagem
{
    public IFormFile? Arquivo { get; set; }
    public string? Titulo { get; set; }
    public string? Executor { get; set; }
}

public sealed record AtendimentoEntrada(decimal Quantidade, string? EquipeCampoCodigo, string? Executor, bool SimularFalha = false);

public sealed record OcorrenciaEntrada(int RegiaoId, int CategoriaId, string? Titulo, string? Descricao,
    string? Endereco, string? Unidade, decimal QuantidadeEstimada, string? Executor);

// ---------- Contratos de saída (JSON) ----------
public sealed record OcorrenciaResumo(int Id, string Titulo, string Regiao, string Bairro, string Categoria,
    string Endereco, string Unidade, decimal QuantidadeEstimada, decimal QuantidadePendente, string Situacao,
    DateTime AbertaUtc, int Imagens, int Atendimentos);

public sealed record ImagemMetadados(int Id, int OcorrenciaId, string Titulo, string NomeArquivo, string Mime,
    int TamanhoBytes, string HashSha256, string EquipeRm, string Executor, DateTime EnviadaUtc);
