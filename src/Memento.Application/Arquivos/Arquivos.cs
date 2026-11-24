namespace Memento.Application.Arquivos;

/// <summary>Arquivo recebido do cliente, já desacoplado do ASP.NET (IFormFile).</summary>
public sealed record ArquivoUpload(string NomeOriginal, long Tamanho, Stream Conteudo);

public sealed record ArquivoConteudo(Stream Conteudo, string ContentType, string NomeOriginal, string Hash);

public sealed class UploadOptions
{
    public const string Secao = "Upload";

    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxArquivosPorRequisicao { get; set; } = 10;
}

public static class ArquivoUrls
{
    /// <summary>Rota autenticada que serve o arquivo (o container do blob é privado).</summary>
    public static string De(Guid arquivoId) => $"/api/arquivos/{arquivoId}";
    public static string? De(Guid? arquivoId) => arquivoId is null ? null : De(arquivoId.Value);
}
