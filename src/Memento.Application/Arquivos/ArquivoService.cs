using System.Security.Cryptography;
using Memento.Application.Abstractions;
using Memento.Application.Common;
using Memento.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Memento.Application.Arquivos;

public sealed class ArquivoService(
    IArquivoRepository arquivos,
    IArquivoStorage storage,
    IOptions<UploadOptions> opcoes,
    TimeProvider relogio,
    ILogger<ArquivoService> logger)
{
    private readonly UploadOptions _opcoes = opcoes.Value;

    /// <summary>
    /// Valida o arquivo, grava o blob (nome = novo GUID) e registra o <see cref="Arquivo"/> no contexto.
    /// Não persiste no banco: quem chama decide quando salvar e, se falhar, descarta o blob com <see cref="DescartarBlobsAsync"/>.
    /// </summary>
    public async Task<Resultado<Arquivo>> EnviarAsync(ArquivoUpload upload, Guid usuarioId, CancellationToken ct = default)
    {
        if (upload.Tamanho <= 0) return Erro.Validacao("arquivo", "O arquivo está vazio.");
        if (upload.Tamanho > _opcoes.MaxBytes) return ErroTamanho();

        var stream = upload.Conteudo;
        MemoryStream? copia = null;
        try
        {
            if (!stream.CanSeek)
            {
                copia = new MemoryStream();
                await stream.CopyToAsync(copia, ct);
                copia.Position = 0;
                stream = copia;
            }

            if (stream.Length == 0) return Erro.Validacao("arquivo", "O arquivo está vazio.");
            if (stream.Length > _opcoes.MaxBytes) return ErroTamanho();

            var cabecalho = new byte[32];
            stream.Position = 0;
            var lidos = await stream.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, ct);
            var contentType = DetectorTipoImagem.Detectar(cabecalho.AsSpan(0, lidos));
            if (contentType is null)
                return Erro.Validacao("arquivo", "Formato não suportado. Envie uma imagem JPEG, PNG, WebP ou HEIC.");

            stream.Position = 0;
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).ToLowerInvariant();

            stream.Position = 0;
            var id = Guid.NewGuid();
            await storage.EnviarAsync(id, stream, contentType, ct);

            var arquivo = Arquivo.Criar(
                id, NomeSeguro(upload.NomeOriginal), stream.Length, contentType, hash, usuarioId, relogio.GetUtcNow());
            arquivos.Adicionar(arquivo);
            return arquivo;
        }
        finally
        {
            copia?.Dispose();
        }
    }

    /// <summary>Remove blobs sem derrubar o fluxo: um blob órfão é tolerável, uma falha para o usuário não.</summary>
    public async Task DescartarBlobsAsync(IEnumerable<Guid> arquivoIds)
    {
        foreach (var id in arquivoIds)
        {
            try
            {
                await storage.ExcluirAsync(id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível excluir o blob {ArquivoId}; ficará órfão no storage.", id);
            }
        }
    }

    public async Task<Resultado<ArquivoConteudo>> ObterConteudoAsync(Guid usuarioId, Guid arquivoId, CancellationToken ct = default)
    {
        var arquivo = await arquivos.ObterPorIdAsync(arquivoId, ct);
        // Sem permissão responde igual a "não existe" para não revelar quais arquivos existem.
        if (arquivo is null || !await arquivos.UsuarioPodeAcessarAsync(arquivoId, usuarioId, ct))
            return Erro.NaoEncontrado("Arquivo não encontrado.");

        var conteudo = await storage.AbrirLeituraAsync(arquivoId, ct);
        if (conteudo is null) return Erro.NaoEncontrado("Arquivo não encontrado.");

        return new ArquivoConteudo(conteudo, arquivo.ContentType, arquivo.NomeOriginal, arquivo.Hash);
    }

    private Erro ErroTamanho()
        => Erro.Validacao("arquivo", $"O arquivo excede o limite de {_opcoes.MaxBytes / 1024 / 1024} MB.");

    private static string NomeSeguro(string? nome)
    {
        var limpo = new string((Path.GetFileName(nome ?? string.Empty)).Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (limpo.Length == 0) return "arquivo";
        return limpo.Length <= 255 ? limpo : limpo[..255];
    }
}
