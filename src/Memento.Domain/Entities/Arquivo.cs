using Memento.Domain.Exceptions;

namespace Memento.Domain.Entities;

/// <summary>
/// Metadados de um arquivo enviado. O <see cref="Id"/> (GUID gerado por nós) é o nome do blob no storage.
/// </summary>
public class Arquivo
{
    private Arquivo() { }

    public Guid Id { get; private set; }
    public string NomeOriginal { get; private set; } = null!;
    public long Tamanho { get; private set; }
    public string ContentType { get; private set; } = null!;
    /// <summary>SHA-256 (hex) do conteúdo.</summary>
    public string Hash { get; private set; } = null!;
    public Guid EnviadoPorId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static Arquivo Criar(
        Guid id, string nomeOriginal, long tamanho, string contentType, string hash,
        Guid enviadoPorId, DateTimeOffset agora)
    {
        if (id == Guid.Empty) throw new DomainException("Identificador do arquivo inválido.");
        if (tamanho <= 0) throw new DomainException("Arquivo vazio.");
        if (string.IsNullOrWhiteSpace(contentType)) throw new DomainException("Tipo do arquivo inválido.");

        return new Arquivo
        {
            Id = id,
            NomeOriginal = nomeOriginal,
            Tamanho = tamanho,
            ContentType = contentType,
            Hash = hash,
            EnviadoPorId = enviadoPorId,
            CriadoEm = agora
        };
    }
}
