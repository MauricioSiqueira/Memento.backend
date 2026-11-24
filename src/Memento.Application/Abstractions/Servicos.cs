using Memento.Domain.Entities;

namespace Memento.Application.Abstractions;

public sealed record AccessToken(string Token, DateTimeOffset ExpiraEm);

public interface ITokenService
{
    TimeSpan ValidadeRefreshToken { get; }
    AccessToken GerarAccessToken(Usuario usuario, Guid sessaoId);
    /// <summary>Token opaco de alta entropia, entregue ao cliente uma única vez.</summary>
    string GerarRefreshToken();
    string HashRefreshToken(string refreshToken);
}

public interface ISenhaHasher
{
    string Hash(string senha);
    bool Verificar(string hash, string senha);
    /// <summary>Executa uma verificação descartável para igualar o tempo de resposta quando o usuário não existe.</summary>
    void SimularVerificacao(string senha);
}

/// <summary>Porta para o armazenamento de blobs. O nome do blob é sempre o GUID do <c>Arquivo</c>.</summary>
public interface IArquivoStorage
{
    Task EnviarAsync(Guid arquivoId, Stream conteudo, string contentType, CancellationToken ct = default);
    /// <summary>Abre o blob para leitura, ou null se não existir.</summary>
    Task<Stream?> AbrirLeituraAsync(Guid arquivoId, CancellationToken ct = default);
    Task ExcluirAsync(Guid arquivoId, CancellationToken ct = default);
}
