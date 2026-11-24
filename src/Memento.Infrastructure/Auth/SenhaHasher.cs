using Memento.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Memento.Infrastructure.Auth;

/// <summary>PBKDF2 (HMAC-SHA512, salt aleatório) via o hasher do ASP.NET Core Identity.</summary>
internal sealed class SenhaHasher : ISenhaHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _contexto = new();
    private readonly Lazy<string> _hashFalso;

    public SenhaHasher() => _hashFalso = new Lazy<string>(() => _hasher.HashPassword(_contexto, Guid.NewGuid().ToString("N")));

    public string Hash(string senha) => _hasher.HashPassword(_contexto, senha);

    public bool Verificar(string hash, string senha)
        => _hasher.VerifyHashedPassword(_contexto, hash, senha) != PasswordVerificationResult.Failed;

    public void SimularVerificacao(string senha) => _hasher.VerifyHashedPassword(_contexto, _hashFalso.Value, senha);
}
