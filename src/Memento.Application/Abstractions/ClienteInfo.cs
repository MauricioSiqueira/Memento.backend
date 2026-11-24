namespace Memento.Application.Abstractions;

/// <summary>Dados do cliente HTTP guardados na sessão (auditoria).</summary>
public sealed record ClienteInfo(string? UserAgent, string? Ip);
