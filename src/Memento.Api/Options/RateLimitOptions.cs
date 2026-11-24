namespace Memento.Api.Options;

public sealed class RateLimitOptions
{
    public const string Secao = "RateLimit";

    /// <summary>Cadastro, login e refresh: por IP e por rota.</summary>
    public LimiteOptions Auth { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    /// <summary>Uploads: por usuário.</summary>
    public LimiteOptions Upload { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
    /// <summary>Todas as demais rotas: por usuário (ou IP se anônimo).</summary>
    public LimiteOptions Api { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };
}

public sealed class LimiteOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
