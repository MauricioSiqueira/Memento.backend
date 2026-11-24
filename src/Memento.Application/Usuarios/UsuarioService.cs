using FluentValidation;
using Memento.Application.Abstractions;
using Memento.Application.Arquivos;
using Memento.Application.Common;
using Memento.Domain.Entities;

namespace Memento.Application.Usuarios;

public sealed class UsuarioService(
    IUsuarioRepository usuarios,
    IArquivoRepository arquivosRepositorio,
    ArquivoService arquivos,
    IUnitOfWork unitOfWork,
    IValidator<AtualizarPerfilRequest> validadorPerfil)
{
    private static readonly Erro NaoEncontrado = Erro.NaoEncontrado("Usuário não encontrado.");

    public async Task<Resultado<UsuarioDto>> ObterAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioId, ct);
        return usuario is null ? NaoEncontrado : UsuarioDto.De(usuario);
    }

    public async Task<Resultado<UsuarioDto>> AtualizarPerfilAsync(
        Guid usuarioId, AtualizarPerfilRequest request, CancellationToken ct = default)
    {
        if (validadorPerfil.ValidarOuErro(request) is { } invalido) return invalido;

        var usuario = await usuarios.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null) return NaoEncontrado;

        var normalizado = Usuario.NormalizarNomeUsuario(request.NomeUsuario);
        if (await usuarios.NomeUsuarioEmUsoAsync(normalizado, usuarioId, ct))
            return Erro.Conflito("Este nome de usuário já está em uso.");

        usuario.AlterarNomeUsuario(request.NomeUsuario);
        await unitOfWork.SalvarAsync(ct);
        return UsuarioDto.De(usuario);
    }

    /// <summary>
    /// Troca a foto de perfil: sobe o novo blob, aponta o usuário para ele (removendo o registro antigo na mesma
    /// transação) e só então apaga o blob antigo. Se algo falhar antes do commit, a foto atual continua intacta.
    /// </summary>
    public async Task<Resultado<UsuarioDto>> TrocarFotoPerfilAsync(
        Guid usuarioId, ArquivoUpload upload, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null) return NaoEncontrado;

        var antigo = usuario.FotoPerfilArquivoId is { } antigoId
            ? await arquivosRepositorio.ObterPorIdAsync(antigoId, ct)
            : null;

        var envio = await arquivos.EnviarAsync(upload, usuarioId, ct);
        if (!envio.Sucesso) return envio.Erro!;
        var novo = envio.Valor;

        usuario.DefinirFotoPerfil(novo.Id);
        if (antigo is not null) arquivosRepositorio.Remover(antigo);

        try
        {
            await unitOfWork.SalvarAsync(ct);
        }
        catch
        {
            await arquivos.DescartarBlobsAsync([novo.Id]);
            throw;
        }

        if (antigo is not null) await arquivos.DescartarBlobsAsync([antigo.Id]);
        return UsuarioDto.De(usuario);
    }

    public async Task<Resultado<UsuarioDto>> RemoverFotoPerfilAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null) return NaoEncontrado;

        if (usuario.FotoPerfilArquivoId is { } fotoId)
        {
            var arquivo = await arquivosRepositorio.ObterPorIdAsync(fotoId, ct);
            usuario.DefinirFotoPerfil(null);
            if (arquivo is not null) arquivosRepositorio.Remover(arquivo);
            await unitOfWork.SalvarAsync(ct);
            await arquivos.DescartarBlobsAsync([fotoId]);
        }

        return UsuarioDto.De(usuario);
    }
}
