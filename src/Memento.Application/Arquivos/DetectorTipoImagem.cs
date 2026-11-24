using System.Text;

namespace Memento.Application.Arquivos;

/// <summary>Detecta o tipo real da imagem pelos magic bytes — o content-type enviado pelo cliente não é confiável.</summary>
public static class DetectorTipoImagem
{
    private static readonly string[] MarcasHeif = ["heic", "heix", "heim", "heis", "hevc", "hevx", "mif1", "msf1"];

    public static string? Detectar(ReadOnlySpan<byte> cabecalho)
    {
        if (cabecalho.Length >= 3 && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
            return "image/jpeg";

        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (cabecalho.Length >= 8 && cabecalho[..8].SequenceEqual(png))
            return "image/png";

        if (cabecalho.Length >= 12
            && cabecalho[..4].SequenceEqual("RIFF"u8)
            && cabecalho[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        if (cabecalho.Length >= 12 && cabecalho[4..8].SequenceEqual("ftyp"u8))
        {
            var marca = Encoding.ASCII.GetString(cabecalho[8..12]);
            if (MarcasHeif.Contains(marca)) return "image/heic";
        }

        return null;
    }
}
