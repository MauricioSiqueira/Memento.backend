using FluentValidation;
using Memento.Application.Abstractions;
using Memento.Application.Common;
using Memento.Application.Usuarios;
using Memento.Domain.Entities;

namespace Memento.Application.Auth;

public sealed class AuthService(
    IUsuarioRepository usuarios,
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    ISenhaHasher hasher,
    ITokenService tokens,
    IValidator<CadastroRequest> validadorCadastro,
    IValidator<LoginRequest> validadorLogin,
    IValidator<RefreshRequest> validadorRefresh,
    TimeProvider relogio)
{
    private static readonly Erro CredenciaisInvalidas = Erro.NaoAutenticado("E-mail ou senha inválidos.");
    private static readonly Erro SessaoInvalida = Erro.NaoAutenticado("Sessão inválida ou expirada. Faça login novamente.");

    public async Task<Resultado<UsuarioDto>> CadastrarAsync(CadastroRequest request, CancellationToken ct = default)
    {
        if (validadorCadastro.ValidarOuErro(request) is { } invalido) return invalido;

        var email = Usuario.NormalizarEmail(request.Email);
        var nomeNormalizado = Usuario.NormalizarNomeUsuario(request.NomeUsuario);

        var detalhes = new Dictionary<string, string[]>();
        if (await usuarios.EmailEmUsoAsync(email, ct)) detalhes["email"] = ["Este e-mail já está em uso."];
        if (await usuarios.NomeUsuarioEmUsoAsync(nomeNormalizado, null, ct))
            detalhes["nomeUsuario"] = ["Este nome de usuário já está em uso."];
        if (detalhes.Count > 0)
            return new Erro(TipoErro.Conflito, "conflito", "E-mail ou nome de usuário já cadastrado.", detalhes);

        var usuario = Usuario.Criar(
            request.NomeUsuario, email, hasher.Hash(request.Senha), request.DataNascimento, relogio.GetUtcNow());
        usuarios.Adicionar(usuario);
        await unitOfWork.SalvarAsync(ct);

        return UsuarioDto.De(usuario);
    }

    public async Task<Resultado<TokensDto>> LoginAsync(LoginRequest request, ClienteInfo cliente, CancellationToken ct = default)
    {
        if (validadorLogin.ValidarOuErro(request) is { } invalido) return invalido;

        var usuario = await usuarios.ObterPorEmailAsync(Usuario.NormalizarEmail(request.Email), ct);
        if (usuario is null)
        {
            // Gasta o mesmo tempo de uma verificação real: não revela se o e-mail existe.
            hasher.SimularVerificacao(request.Senha);
            return CredenciaisInvalidas;
        }

        if (!hasher.Verificar(usuario.SenhaHash, request.Senha)) return CredenciaisInvalidas;

        var agora = relogio.GetUtcNow();
        var refreshToken = tokens.GerarRefreshToken();
        var expiraEm = agora + tokens.ValidadeRefreshToken;
        var sessao = Sessao.Criar(usuario.Id, tokens.HashRefreshToken(refreshToken), expiraEm, cliente.UserAgent, cliente.Ip, agora);
        sessoes.Adicionar(sessao);
        await unitOfWork.SalvarAsync(ct);

        return MontarTokens(usuario, sessao, refreshToken);
    }

    public async Task<Resultado<TokensDto>> RenovarAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (validadorRefresh.ValidarOuErro(request) is { } invalido) return invalido;

        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var sessao = await sessoes.ObterPorRefreshHashAsync(hash, ct);
        if (sessao is null) return SessaoInvalida;

        var agora = relogio.GetUtcNow();

        // Um token já rotacionado voltou a ser usado: provável vazamento. Derruba a sessão inteira.
        if (sessao.ReusoDeTokenAnterior(hash))
        {
            sessao.Revogar(agora);
            await unitOfWork.SalvarAsync(ct);
            return SessaoInvalida;
        }

        if (!sessao.EstaAtiva(agora)) return SessaoInvalida;

        var usuario = await usuarios.ObterPorIdAsync(sessao.UsuarioId, ct);
        if (usuario is null) return SessaoInvalida;

        var novoRefresh = tokens.GerarRefreshToken();
        sessao.Rotacionar(tokens.HashRefreshToken(novoRefresh), agora + tokens.ValidadeRefreshToken);
        await unitOfWork.SalvarAsync(ct);

        return MontarTokens(usuario, sessao, novoRefresh);
    }

    /// <summary>Revoga a sessão do dispositivo. Idempotente.</summary>
    public async Task<Resultado> SairAsync(Guid sessaoId, CancellationToken ct = default)
    {
        var sessao = await sessoes.ObterPorIdAsync(sessaoId, ct);
        if (sessao is not null)
        {
            sessao.Revogar(relogio.GetUtcNow());
            await unitOfWork.SalvarAsync(ct);
        }
        return Resultado.Ok();
    }

    private TokensDto MontarTokens(Usuario usuario, Sessao sessao, string refreshToken)
    {
        var access = tokens.GerarAccessToken(usuario, sessao.Id);
        return new TokensDto(access.Token, access.ExpiraEm, refreshToken, sessao.ExpiraEm, UsuarioDto.De(usuario));
    }
}
