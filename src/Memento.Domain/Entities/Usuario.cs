using Memento.Domain.Exceptions;

namespace Memento.Domain.Entities;

public class Usuario
{
    private Usuario() { }

    public Guid Id { get; private set; }
    public string NomeUsuario { get; private set; } = null!;
    public string NomeUsuarioNormalizado { get; private set; } = null!;
    /// <summary>Sempre armazenado em minúsculas.</summary>
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public DateOnly DataNascimento { get; private set; }
    public Guid? FotoPerfilArquivoId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
    public static string NormalizarNomeUsuario(string nome) => nome.Trim().ToLowerInvariant();

    public static Usuario Criar(
        string nomeUsuario, string email, string senhaHash, DateOnly dataNascimento, DateTimeOffset agora)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            SenhaHash = senhaHash,
            DataNascimento = dataNascimento,
            CriadoEm = agora
        };
        usuario.AlterarNomeUsuario(nomeUsuario);
        usuario.Email = NormalizarEmail(email);
        return usuario;
    }

    public void AlterarNomeUsuario(string nomeUsuario)
    {
        var nome = nomeUsuario?.Trim();
        if (string.IsNullOrEmpty(nome)) throw new DomainException("Informe um nome de usuário.");
        NomeUsuario = nome;
        NomeUsuarioNormalizado = NormalizarNomeUsuario(nome);
    }

    public void DefinirFotoPerfil(Guid? arquivoId) => FotoPerfilArquivoId = arquivoId;
}
