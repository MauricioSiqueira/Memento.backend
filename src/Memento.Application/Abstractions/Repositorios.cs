using Memento.Application.Albuns;
using Memento.Domain.Entities;

namespace Memento.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SalvarAsync(CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default);
    Task<bool> EmailEmUsoAsync(string emailNormalizado, CancellationToken ct = default);
    Task<bool> NomeUsuarioEmUsoAsync(string nomeNormalizado, Guid? ignorarUsuarioId, CancellationToken ct = default);
    void Adicionar(Usuario usuario);
}

public interface ISessaoRepository
{
    Task<Sessao?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Encontra a sessão cujo refresh token atual OU anterior tem o hash informado.</summary>
    Task<Sessao?> ObterPorRefreshHashAsync(string hash, CancellationToken ct = default);
    void Adicionar(Sessao sessao);
}

public interface IArquivoRepository
{
    Task<Arquivo?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    void Adicionar(Arquivo arquivo);
    void Remover(Arquivo arquivo);
    /// <summary>Quem enviou, membro de álbum que contém o arquivo, ou foto de perfil de si/de quem divide álbum.</summary>
    Task<bool> UsuarioPodeAcessarAsync(Guid arquivoId, Guid usuarioId, CancellationToken ct = default);
}

public interface IAlbumRepository
{
    /// <summary>Carrega o álbum rastreado, com os membros.</summary>
    Task<Album?> ObterComMembrosAsync(Guid id, CancellationToken ct = default);
    Task<Album?> ObterPorTokenConviteAsync(string token, CancellationToken ct = default);
    void Adicionar(Album album);
    void Remover(Album album);

    Task<AlbumArquivo?> ObterFotoAsync(Guid albumId, Guid arquivoId, CancellationToken ct = default);
    void AdicionarFoto(AlbumArquivo foto);
    void RemoverFoto(AlbumArquivo foto);
    Task<IReadOnlyList<Arquivo>> ListarArquivosDoAlbumAsync(Guid albumId, CancellationToken ct = default);

    Task<IReadOnlyList<AlbumResumoDto>> ListarResumosAsync(Guid usuarioId, CancellationToken ct = default);
    Task<AlbumDetalheDto?> ObterDetalheAsync(Guid albumId, Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<MembroDto>> ListarMembrosAsync(Guid albumId, CancellationToken ct = default);
}
