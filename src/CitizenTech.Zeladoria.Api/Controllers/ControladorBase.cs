using Microsoft.AspNetCore.Mvc;

namespace CitizenTech.Zeladoria.Api.Controllers;

/// <summary>Respostas de erro padronizadas (ProblemDetails, application/problem+json).</summary>
public abstract class ControladorBase : ControllerBase
{
    protected ObjectResult Falha(int status, string titulo, string detalhe) =>
        Problem(detail: detalhe, statusCode: status, title: titulo);

    /// <summary>Quantidade positiva, até 999.999.999,999 e no máximo 3 casas (decimal(12,3) do banco).</summary>
    protected static bool QuantidadeValida(decimal q) =>
        q > 0 && q <= 999_999_999.999m && decimal.Round(q, 3) == q;
}
