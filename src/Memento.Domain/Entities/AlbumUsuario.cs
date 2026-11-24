using Memento.Domain.Enums;

namespace Memento.Domain.Entities;

/// <summary>Tabela de ligação n-n entre álbum e usuário. O favorito é pessoal de cada participante.</summary>
public class AlbumUsuario
{
    private AlbumUsuario() { }

    public Guid AlbumId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public PapelAlbum Papel { get; private set; }
    public bool Favorito { get; private set; }
    public DateTimeOffset EntrouEm { get; private set; }

    public static AlbumUsuario Criar(Guid albumId, Guid usuarioId, PapelAlbum papel, DateTimeOffset agora)
        => new() { AlbumId = albumId, UsuarioId = usuarioId, Papel = papel, EntrouEm = agora };

    public bool EhDono => Papel == PapelAlbum.Dono;

    public void DefinirFavorito(bool favorito) => Favorito = favorito;
}
