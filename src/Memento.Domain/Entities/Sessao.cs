namespace Memento.Domain.Entities;

/// <summary>
/// Sessão de login de um dispositivo. O refresh token (opaco) nunca é guardado em claro: só o hash.
/// A cada renovação o token é rotacionado; o hash anterior é mantido para detectar reuso (token roubado).
/// </summary>
public class Sessao
{
    private Sessao() { }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string RefreshTokenHash { get; private set; } = null!;
    public string? RefreshTokenAnteriorHash { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? RevogadaEm { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Ip { get; private set; }

    public static Sessao Criar(
        Guid usuarioId, string refreshTokenHash, DateTimeOffset expiraEm, string? userAgent, string? ip, DateTimeOffset agora)
        => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            RefreshTokenHash = refreshTokenHash,
            CriadaEm = agora,
            ExpiraEm = expiraEm,
            UserAgent = Truncar(userAgent, 300),
            Ip = Truncar(ip, 64)
        };

    public bool EstaAtiva(DateTimeOffset agora) => RevogadaEm is null && ExpiraEm > agora;

    public bool ReusoDeTokenAnterior(string hashApresentado)
        => RefreshTokenAnteriorHash is not null && RefreshTokenAnteriorHash == hashApresentado;

    public void Rotacionar(string novoHash, DateTimeOffset novaExpiracao)
    {
        RefreshTokenAnteriorHash = RefreshTokenHash;
        RefreshTokenHash = novoHash;
        ExpiraEm = novaExpiracao;
    }

    public void Revogar(DateTimeOffset agora) => RevogadaEm ??= agora;

    private static string? Truncar(string? valor, int max)
        => valor is null ? null : valor.Length <= max ? valor : valor[..max];
}
