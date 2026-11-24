using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Memento.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Memento.Infrastructure.Storage;

/// <summary>Guarda os arquivos num container privado do Azure Blob Storage (Azurite em dev). Nome do blob = GUID.</summary>
internal sealed class BlobArquivoStorage(BlobServiceClient servico, IOptions<BlobStorageOptions> opcoes) : IArquivoStorage
{
    private readonly BlobContainerClient _container = servico.GetBlobContainerClient(opcoes.Value.Container);
    private readonly SemaphoreSlim _trava = new(1, 1);
    private volatile bool _containerPronto;

    public async Task EnviarAsync(Guid arquivoId, Stream conteudo, string contentType, CancellationToken ct = default)
    {
        await GarantirContainerAsync(ct);
        var opcoesUpload = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await _container.GetBlobClient(Nome(arquivoId)).UploadAsync(conteudo, opcoesUpload, ct);
    }

    public async Task<Stream?> AbrirLeituraAsync(Guid arquivoId, CancellationToken ct = default)
    {
        await GarantirContainerAsync(ct);
        try
        {
            return await _container.GetBlobClient(Nome(arquivoId)).OpenReadAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task ExcluirAsync(Guid arquivoId, CancellationToken ct = default)
    {
        await GarantirContainerAsync(ct);
        await _container.GetBlobClient(Nome(arquivoId)).DeleteIfExistsAsync(cancellationToken: ct);
    }

    private static string Nome(Guid id) => id.ToString("N");

    // Criação preguiçosa (e repetida em caso de falha): a API sobe mesmo com o Azurite desligado.
    private async Task GarantirContainerAsync(CancellationToken ct)
    {
        if (_containerPronto) return;
        await _trava.WaitAsync(ct);
        try
        {
            if (_containerPronto) return;
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            _containerPronto = true;
        }
        finally
        {
            _trava.Release();
        }
    }
}
