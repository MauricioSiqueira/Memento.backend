namespace Memento.Api;

public static class PoliticasRateLimit
{
    public const string Auth = "auth";
    public const string Upload = "upload";
}

public static class LimitesUpload
{
    /// <summary>Teto do corpo multipart de uma requisição (várias fotos). O limite por arquivo vem de <c>Upload:MaxBytes</c>.</summary>
    public const long CorpoMaximoBytes = 60L * 1024 * 1024;
}
