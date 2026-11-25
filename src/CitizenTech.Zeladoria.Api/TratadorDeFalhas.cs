using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace CitizenTech.Zeladoria.Api;

/// <summary>
/// Tratamento centralizado: banco indisponível vira 503 e qualquer outra falha vira 500,
/// sempre em ProblemDetails JSON, sem expor stack trace nem string de conexão ao cliente.
/// </summary>
public sealed class TratadorDeFalhas(IProblemDetailsService problemas, ILogger<TratadorDeFalhas> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        var (status, titulo, detalhe) = BancoIndisponivel(excecao)
            ? (StatusCodes.Status503ServiceUnavailable, "Banco de dados indisponível",
               "Não foi possível acessar o banco agora. Nenhuma alteração foi confirmada. Tente novamente em instantes.")
            : (StatusCodes.Status500InternalServerError, "Falha interna",
               "Erro inesperado. Os detalhes ficam apenas no log local do servidor.");

        log.LogError(excecao, "Falha tratada ({Status}) em {Metodo} {Caminho}", status, contexto.Request.Method, contexto.Request.Path);
        contexto.Response.StatusCode = status;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = { Status = status, Title = titulo, Detail = detalhe }
        });
    }

    public static bool BancoIndisponivel(Exception? e)
    {
        for (; e is not null; e = e.InnerException)
            if (e is DbException or TimeoutException) return true;
        return false;
    }
}
