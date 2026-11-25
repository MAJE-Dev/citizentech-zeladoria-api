namespace CitizenTech.Zeladoria.Api;

/// <summary>
/// Verificação simples de coerência entre extensão, MIME declarado e assinatura (magic bytes) de PNG/JPEG.
/// Não é decodificador completo nem mecanismo de segurança de produção.
/// </summary>
public static class ValidadorImagem
{
    public const int MaxBytes = 2 * 1024 * 1024; // 2 MiB por imagem
    private static readonly byte[] AssinaturaPng = [137, 80, 78, 71, 13, 10, 26, 10];

    /// <summary>Retorna image/png ou image/jpeg conforme os primeiros bytes; null se não for nenhum dos dois.</summary>
    public static string? DetectarMime(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(AssinaturaPng)) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        return null;
    }

    /// <summary>Extensão, Content-Type e assinatura precisam apontar para o mesmo formato aceito.</summary>
    public static bool FormatoCoerente(string nomeArquivo, string? contentType, ReadOnlySpan<byte> bytes, out string mime)
    {
        mime = DetectarMime(bytes) ?? "";
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        return mime switch
        {
            "image/png" => ext == ".png" && contentType == "image/png",
            "image/jpeg" => (ext is ".jpg" or ".jpeg") && contentType == "image/jpeg",
            _ => false
        };
    }

    /// <summary>Remove caminho enviado pelo cliente (C:\..\foto.png -> foto.png).</summary>
    public static string NomeSeguro(string? nomeOriginal) =>
        Path.GetFileName((nomeOriginal ?? "").Replace('\\', '/')).Trim();
}
