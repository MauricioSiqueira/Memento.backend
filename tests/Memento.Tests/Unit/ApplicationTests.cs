using FluentAssertions;
using Memento.Application.Albuns;
using Memento.Application.Arquivos;
using Memento.Application.Auth;
using Microsoft.Extensions.Time.Testing;

namespace Memento.Tests.Unit;

public class ApplicationTests
{
    private static CadastroRequest Valido() => new("ana", "ana@teste.com", "Senha@123", "Senha@123", new DateOnly(1995, 3, 10));

    private static CadastroRequestValidator Validador()
        => new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Cadastro_valido_passa() => Validador().Validate(Valido()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("curta1!A")]            // válida (8)
    public void Senha_no_limite_inferior_e_aceita(string senha)
        => Validador().Validate(Valido() with { Senha = senha, ConfirmarSenha = senha }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("Ab1!")]                 // curta
    [InlineData("senha@1234")]           // sem maiúscula
    [InlineData("SENHA@1234")]           // sem minúscula
    [InlineData("Senha@abcd")]           // sem número
    [InlineData("Senha12345")]           // sem símbolo
    [InlineData("Senha@1234567890123")]  // longa demais
    public void Senha_fora_da_regra_e_recusada(string senha)
        => Validador().Validate(Valido() with { Senha = senha, ConfirmarSenha = senha })
            .Errors.Should().Contain(e => e.PropertyName == nameof(CadastroRequest.Senha));

    [Fact]
    public void Confirmacao_diferente_e_recusada()
        => Validador().Validate(Valido() with { ConfirmarSenha = "Outra@123" })
            .Errors.Should().Contain(e => e.PropertyName == nameof(CadastroRequest.ConfirmarSenha));

    [Theory]
    [InlineData("ab")]
    [InlineData("com espaco")]
    public void Nome_de_usuario_invalido_e_recusado(string nome)
        => Validador().Validate(Valido() with { NomeUsuario = nome })
            .Errors.Should().Contain(e => e.PropertyName == nameof(CadastroRequest.NomeUsuario));

    [Fact]
    public void Data_de_nascimento_no_futuro_ou_antes_de_1900_e_recusada()
    {
        Validador().Validate(Valido() with { DataNascimento = new DateOnly(2030, 1, 1) }).IsValid.Should().BeFalse();
        Validador().Validate(Valido() with { DataNascimento = new DateOnly(1899, 12, 31) }).IsValid.Should().BeFalse();
        Validador().Validate(Valido() with { DataNascimento = default }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("memento://convite/Af6M126c5c2p37S62vFoTxIndORzR6TBWo97zB_T7RE")]
    [InlineData("https://memento.app/convite/Af6M126c5c2p37S62vFoTxIndORzR6TBWo97zB_T7RE?x=1")]
    [InlineData("  Af6M126c5c2p37S62vFoTxIndORzR6TBWo97zB_T7RE  ")]
    public void Convite_extrai_o_token_de_links_e_do_token_puro(string texto)
        => ConviteLink.ExtrairToken(texto).Should().Be("Af6M126c5c2p37S62vFoTxIndORzR6TBWo97zB_T7RE");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("memento://album/1")]
    [InlineData("memento://convite/curto")]
    [InlineData("https://google.com")]
    public void Convite_rejeita_textos_que_nao_sao_convites(string? texto)
        => ConviteLink.ExtrairToken(texto).Should().BeNull();

    [Fact]
    public void Detector_reconhece_imagens_pelos_magic_bytes()
    {
        DetectorTipoImagem.Detectar([0xFF, 0xD8, 0xFF, 0xE0, 0, 0]).Should().Be("image/jpeg");
        DetectorTipoImagem.Detectar([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0]).Should().Be("image/png");
        DetectorTipoImagem.Detectar("RIFF\0\0\0\0WEBPVP8 "u8).Should().Be("image/webp");
        DetectorTipoImagem.Detectar("\0\0\0\u0018ftypheic\0\0\0\0"u8).Should().Be("image/heic");
        DetectorTipoImagem.Detectar("MZ executavel"u8).Should().BeNull();
        DetectorTipoImagem.Detectar("<svg onload=alert(1)>"u8).Should().BeNull();
    }
}
