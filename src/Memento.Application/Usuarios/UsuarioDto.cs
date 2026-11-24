using Memento.Application.Arquivos;
using Memento.Domain.Entities;

namespace Memento.Application.Usuarios;

public sealed record UsuarioDto(
    Guid Id,
    string NomeUsuario,
    string Email,
    DateOnly DataNascimento,
    string? FotoPerfilUrl)
{
    public static UsuarioDto De(Usuario u)
        => new(u.Id, u.NomeUsuario, u.Email, u.DataNascimento, ArquivoUrls.De(u.FotoPerfilArquivoId));
}

public sealed record AtualizarPerfilRequest(string NomeUsuario);
