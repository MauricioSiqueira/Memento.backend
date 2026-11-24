namespace Memento.Domain.Exceptions;

/// <summary>Violação de uma regra de negócio protegida pelo domínio.</summary>
public sealed class DomainException(string message) : Exception(message);
