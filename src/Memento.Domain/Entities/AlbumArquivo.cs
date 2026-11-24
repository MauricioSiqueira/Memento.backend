namespace Memento.Domain.Entities;

/// <summary>Liga uma foto (<see cref="Arquivo"/>) a um álbum.</summary>
public class AlbumArquivo
{
    private AlbumArquivo() { }

    public Guid AlbumId { get; private set; }
    public Guid ArquivoId { get; private set; }
    public Guid AdicionadoPorId { get; private set; }
    public DateTimeOffset AdicionadoEm { get; private set; }

    public Arquivo Arquivo { get; private set; } = null!;

    public static AlbumArquivo Criar(Guid albumId, Guid arquivoId, Guid adicionadoPorId, DateTimeOffset agora)
        => new() { AlbumId = albumId, ArquivoId = arquivoId, AdicionadoPorId = adicionadoPorId, AdicionadoEm = agora };
}
