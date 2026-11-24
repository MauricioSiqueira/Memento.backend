namespace Memento.Application.Albuns;

public sealed record CriarAlbumRequest(string Nome);
public sealed record RenomearAlbumRequest(string Nome);
public sealed record FavoritoRequest(bool Favorito);
public sealed record EntrarAlbumRequest(string Link);

public sealed record AlbumResumoDto(
    Guid Id,
    string Nome,
    bool Favorito,
    bool EhDono,
    int TotalFotos,
    int TotalMembros,
    string? CapaUrl,
    DateTimeOffset CriadoEm);

public sealed record FotoDto(Guid ArquivoId, string Url, Guid AdicionadoPorId, DateTimeOffset AdicionadoEm);

public sealed record AlbumDetalheDto(
    Guid Id,
    string Nome,
    bool Favorito,
    bool EhDono,
    int TotalMembros,
    string? CapaUrl,
    DateTimeOffset CriadoEm,
    IReadOnlyList<FotoDto> Fotos);

public sealed record MembroDto(
    Guid UsuarioId,
    string NomeUsuario,
    string? FotoPerfilUrl,
    string Papel,
    DateTimeOffset EntrouEm);

public sealed record ConviteDto(string Link);
