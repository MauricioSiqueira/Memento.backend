namespace Memento.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string Secao = "Storage";

    /// <summary>Em desenvolvimento: <c>UseDevelopmentStorage=true</c> (Azurite).</summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
    public string Container { get; set; } = "memento-arquivos";
}
