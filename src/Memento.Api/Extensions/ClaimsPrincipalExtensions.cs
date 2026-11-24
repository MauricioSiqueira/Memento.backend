using System.Security.Claims;
using Memento.Infrastructure.Auth;

namespace Memento.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid UsuarioId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedAccessException("Token sem identificação do usuário.");

    public static Guid? SessaoId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue(ClaimNomes.Sessao), out var id) ? id : null;
}
