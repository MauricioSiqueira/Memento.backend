using FluentAssertions;
using Memento.Domain.Entities;
using Memento.Domain.Enums;
using Memento.Domain.Exceptions;

namespace Memento.Tests.Unit;

public class DominioTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Album_criado_tem_o_criador_como_dono_e_um_token_de_convite()
    {
        var donoId = Guid.NewGuid();
        var album = Album.Criar("  Viagem  ", donoId, Agora);

        album.Nome.Should().Be("Viagem");
        album.TokenConvite.Should().HaveLength(43).And.MatchRegex("^[A-Za-z0-9_-]+$");
        album.ObterMembro(donoId)!.Papel.Should().Be(PapelAlbum.Dono);
    }

    [Fact]
    public void Adicionar_membro_e_idempotente()
    {
        var album = Album.Criar("Viagem", Guid.NewGuid(), Agora);
        var convidado = Guid.NewGuid();

        album.AdicionarMembro(convidado, Agora);
        album.AdicionarMembro(convidado, Agora);

        album.Membros.Should().HaveCount(2);
        album.ObterMembro(convidado)!.Papel.Should().Be(PapelAlbum.Membro);
    }

    [Fact]
    public void Dono_nao_pode_ser_removido_do_album()
    {
        var donoId = Guid.NewGuid();
        var album = Album.Criar("Viagem", donoId, Agora);

        var acao = () => album.RemoverMembro(donoId);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Regenerar_convite_troca_o_token()
    {
        var album = Album.Criar("Viagem", Guid.NewGuid(), Agora);
        var antigo = album.TokenConvite;

        album.RegenerarConvite().Should().NotBe(antigo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Album_exige_nome(string nome)
    {
        var acao = () => Album.Criar(nome, Guid.NewGuid(), Agora);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Sessao_detecta_reuso_do_refresh_token_anterior()
    {
        var sessao = Sessao.Criar(Guid.NewGuid(), "hash-1", Agora.AddDays(30), "ua", "127.0.0.1", Agora);

        sessao.Rotacionar("hash-2", Agora.AddDays(30));

        sessao.ReusoDeTokenAnterior("hash-1").Should().BeTrue();
        sessao.ReusoDeTokenAnterior("hash-2").Should().BeFalse();
    }

    [Fact]
    public void Sessao_revogada_ou_expirada_nao_esta_ativa()
    {
        var sessao = Sessao.Criar(Guid.NewGuid(), "h", Agora.AddDays(1), null, null, Agora);
        sessao.EstaAtiva(Agora).Should().BeTrue();
        sessao.EstaAtiva(Agora.AddDays(2)).Should().BeFalse();

        sessao.Revogar(Agora);
        sessao.EstaAtiva(Agora).Should().BeFalse();
    }

    [Fact]
    public void Usuario_normaliza_email_e_nome()
    {
        var usuario = Usuario.Criar("Ana", "  Ana@Teste.COM ", "hash", new DateOnly(1995, 3, 10), Agora);

        usuario.Email.Should().Be("ana@teste.com");
        usuario.NomeUsuarioNormalizado.Should().Be("ana");
    }
}
