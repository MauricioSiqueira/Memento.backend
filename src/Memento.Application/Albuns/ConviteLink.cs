using System.Text.RegularExpressions;

namespace Memento.Application.Albuns;

/// <summary>O QR Code do álbum é só este link: <c>memento://convite/{token}</c>.</summary>
public static partial class ConviteLink
{
    public const string Prefixo = "memento://convite/";

    public static string Montar(string token) => Prefixo + token;

    /// <summary>Aceita o link do QR (qualquer esquema, ex. https://.../convite/{token}) ou o token puro.</summary>
    public static string? ExtrairToken(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var valor = texto.Trim();

        var m = RegexLink().Match(valor);
        if (m.Success) return m.Groups[1].Value;

        return RegexToken().IsMatch(valor) ? valor : null;
    }

    [GeneratedRegex(@"convite/([A-Za-z0-9_-]{43})(?:[/?#].*)?$")]
    private static partial Regex RegexLink();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{43}$")]
    private static partial Regex RegexToken();
}
