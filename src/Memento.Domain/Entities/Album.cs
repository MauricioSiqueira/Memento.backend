using System.Security.Cryptography;
using Memento.Domain.Enums;
using Memento.Domain.Exceptions;

namespace Memento.Domain.Entities;

public class Album
{
    public const int TamanhoMaximoNome = 100;

    private readonly List<AlbumUsuario> _membros = [];

    private Album() { }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public Guid DonoId { get; private set; }
    /// <summary>Token do link de convite (32 bytes aleatórios em base64url). Quem tiver o link entra no álbum.</summary>
    public string TokenConvite { get; private set; } = null!;
    public DateTimeOffset CriadoEm { get; private set; }

    public IReadOnlyCollection<AlbumUsuario> Membros => _membros;

    public static Album Criar(string nome, Guid donoId, DateTimeOffset agora)
    {
        var album = new Album
        {
            Id = Guid.NewGuid(),
            DonoId = donoId,
            CriadoEm = agora,
            TokenConvite = GerarToken()
        };
        album.Renomear(nome);
        album._membros.Add(AlbumUsuario.Criar(album.Id, donoId, PapelAlbum.Dono, agora));
        return album;
    }

    public void Renomear(string nome)
    {
        var valor = nome?.Trim();
        if (string.IsNullOrEmpty(valor)) throw new DomainException("Informe o nome do álbum.");
        if (valor.Length > TamanhoMaximoNome)
            throw new DomainException($"O nome do álbum deve ter no máximo {TamanhoMaximoNome} caracteres.");
        Nome = valor;
    }

    public AlbumUsuario? ObterMembro(Guid usuarioId) => _membros.FirstOrDefault(m => m.UsuarioId == usuarioId);

    /// <summary>Adiciona o usuário como Membro. Idempotente: se já participa, devolve o vínculo existente.</summary>
    public AlbumUsuario AdicionarMembro(Guid usuarioId, DateTimeOffset agora)
    {
        var existente = ObterMembro(usuarioId);
        if (existente is not null) return existente;

        var novo = AlbumUsuario.Criar(Id, usuarioId, PapelAlbum.Membro, agora);
        _membros.Add(novo);
        return novo;
    }

    public void RemoverMembro(Guid usuarioId)
    {
        var membro = ObterMembro(usuarioId);
        if (membro is null) return;
        if (membro.EhDono) throw new DomainException("O dono não pode sair do álbum. Exclua o álbum para encerrá-lo.");
        _membros.Remove(membro);
    }

    public string RegenerarConvite()
    {
        TokenConvite = GerarToken();
        return TokenConvite;
    }

    private static string GerarToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
