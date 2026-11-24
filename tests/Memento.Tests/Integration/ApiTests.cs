using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Memento.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class ApiTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/api/albuns")]
    [InlineData("/api/usuarios/me")]
    [InlineData("/api/arquivos/00000000-0000-0000-0000-000000000001")]
    public async Task Rotas_protegidas_exigem_token(string rota)
    {
        var resposta = await factory.CreateClient().GetAsync(rota);
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cadastro_recusa_dados_invalidos_e_duplicados()
    {
        var cliente = factory.CreateClient();

        var invalido = await cliente.PostAsJsonAsync("/api/auth/cadastro", new
        {
            nomeUsuario = "a b", email = "x", senha = "123", confirmarSenha = "1", dataNascimento = "2999-01-01"
        });
        invalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalido.LerAsync())["errors"]!.AsObject().Select(p => p.Key)
            .Should().Contain(["nomeUsuario", "email", "senha", "confirmarSenha", "dataNascimento"]);

        var usuario = await factory.CriarUsuarioAsync("dup");
        var me = await (await usuario.Http.GetAsync("/api/usuarios/me")).LerAsync();
        var duplicado = await cliente.PostAsJsonAsync("/api/auth/cadastro", new
        {
            nomeUsuario = "outro" + Guid.NewGuid().ToString("N")[..6],
            email = me["email"]!.GetValue<string>().ToUpperInvariant(),
            senha = ApiClientExtensions.Senha, confirmarSenha = ApiClientExtensions.Senha, dataNascimento = "1990-01-01"
        });
        duplicado.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_com_senha_errada_ou_email_inexistente_responde_igual()
    {
        var usuario = await factory.CriarUsuarioAsync("log");
        var email = (await (await usuario.Http.GetAsync("/api/usuarios/me")).LerAsync())["email"]!.GetValue<string>();
        var cliente = factory.CreateClient();

        var senhaErrada = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = "Errada@123" });
        var inexistente = await cliente.PostAsJsonAsync("/api/auth/login", new { email = "nao.existe@teste.com", senha = "Errada@123" });

        senhaErrada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        inexistente.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await senhaErrada.LerAsync())["title"]!.ToString().Should().Be((await inexistente.LerAsync())["title"]!.ToString());
    }

    [Fact]
    public async Task Refresh_rotaciona_e_o_reuso_do_token_antigo_revoga_a_sessao()
    {
        var usuario = await factory.CriarUsuarioAsync("ref");
        var cliente = factory.CreateClient();

        var renovado = await cliente.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = usuario.RefreshToken });
        renovado.StatusCode.Should().Be(HttpStatusCode.OK);
        var novoRefresh = (await renovado.LerAsync())["refreshToken"]!.GetValue<string>();
        novoRefresh.Should().NotBe(usuario.RefreshToken);

        // Reapresentar o token antigo = possível vazamento: nega e derruba a sessão.
        (await cliente.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = usuario.RefreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await cliente.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = novoRefresh }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revoga_a_sessao()
    {
        var usuario = await factory.CriarUsuarioAsync("out");

        (await usuario.Http.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = usuario.RefreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Fluxo_de_compartilhamento_por_convite()
    {
        var ana = await factory.CriarUsuarioAsync("ana");
        var bia = await factory.CriarUsuarioAsync("bia");
        var caio = await factory.CriarUsuarioAsync("caio");

        var albumId = await ana.CriarAlbumAsync();
        var envio = await ana.Http.PostAsync($"/api/albuns/{albumId}/fotos",
            ApiClientExtensions.Multipart(("arquivos", "1.png", Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="))));
        envio.StatusCode.Should().Be(HttpStatusCode.Created);
        var arquivoId = (await envio.LerAsync())[0]!["arquivoId"]!.GetValue<string>();

        // Quem não participa não enxerga nem o álbum nem a foto.
        (await bia.Http.GetAsync($"/api/albuns/{albumId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await bia.Http.GetAsync($"/api/arquivos/{arquivoId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Entrar pelo link (o conteúdo do QR Code) é idempotente.
        var link = await ana.ObterLinkConviteAsync(albumId);
        link.Should().StartWith("memento://convite/");
        for (var i = 0; i < 2; i++)
        {
            var entrada = await bia.Http.PostAsJsonAsync("/api/albuns/entrar", new { link });
            entrada.StatusCode.Should().Be(HttpStatusCode.OK);
            (await entrada.LerAsync())["totalMembros"]!.GetValue<int>().Should().Be(2);
        }
        (await bia.Http.GetAsync($"/api/arquivos/{arquivoId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Membro não administra o álbum, mas favorita só para si.
        (await bia.Http.DeleteAsync($"/api/albuns/{albumId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await bia.Http.PostAsync($"/api/albuns/{albumId}/convite/regenerar", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await bia.Http.PutAsJsonAsync($"/api/albuns/{albumId}/favorito", new { favorito = true });
        (await (await ana.Http.GetAsync($"/api/albuns/{albumId}")).LerAsync())["favorito"]!.GetValue<bool>().Should().BeFalse();
        (await (await bia.Http.GetAsync($"/api/albuns/{albumId}")).LerAsync())["favorito"]!.GetValue<bool>().Should().BeTrue();

        // Regenerar o convite invalida o link antigo.
        (await ana.Http.PostAsync($"/api/albuns/{albumId}/convite/regenerar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await caio.Http.PostAsJsonAsync("/api/albuns/entrar", new { link })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await caio.Http.PostAsJsonAsync("/api/albuns/entrar", new { link = "memento://album/1" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Membro sai; o dono não pode sair.
        (await bia.Http.DeleteAsync($"/api/albuns/{albumId}/membros/{bia.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await bia.Http.GetAsync($"/api/albuns/{albumId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ana.Http.DeleteAsync($"/api/albuns/{albumId}/membros/{ana.Id}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Membro_so_remove_as_proprias_fotos()
    {
        var ana = await factory.CriarUsuarioAsync("ana");
        var bia = await factory.CriarUsuarioAsync("bia");
        var albumId = await ana.CriarAlbumAsync();
        await bia.Http.PostAsJsonAsync("/api/albuns/entrar", new { link = await ana.ObterLinkConviteAsync(albumId) });

        var fotoAna = (await (await ana.Http.PostAsync($"/api/albuns/{albumId}/fotos", ApiClientExtensions.Imagem("arquivos", 1))).LerAsync())[0]!["arquivoId"]!.GetValue<string>();
        var fotoBia = (await (await bia.Http.PostAsync($"/api/albuns/{albumId}/fotos", ApiClientExtensions.Imagem("arquivos", 2))).LerAsync())[0]!["arquivoId"]!.GetValue<string>();

        (await bia.Http.DeleteAsync($"/api/albuns/{albumId}/fotos/{fotoAna}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await bia.Http.DeleteAsync($"/api/albuns/{albumId}/fotos/{fotoBia}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ana.Http.DeleteAsync($"/api/albuns/{albumId}/fotos/{fotoAna}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.Storage.Blobs.Keys.Should().NotContain([Guid.Parse(fotoAna), Guid.Parse(fotoBia)]);
    }

    [Fact]
    public async Task Troca_de_foto_de_perfil_apaga_o_blob_antigo_e_mantem_so_o_novo()
    {
        var usuario = await factory.CriarUsuarioAsync("perfil");
        var blobsAntes = factory.Storage.Blobs.Count;

        var primeira = await (await usuario.Http.PutAsync("/api/usuarios/me/foto", ApiClientExtensions.Imagem("arquivo", 1))).LerAsync();
        var urlPrimeira = primeira["fotoPerfilUrl"]!.GetValue<string>();
        var idPrimeira = Guid.Parse(urlPrimeira.Split('/')[^1]);
        factory.Storage.Blobs.Should().ContainKey(idPrimeira);

        var segunda = await (await usuario.Http.PutAsync("/api/usuarios/me/foto", ApiClientExtensions.Imagem("arquivo", 2))).LerAsync();
        var idSegunda = Guid.Parse(segunda["fotoPerfilUrl"]!.GetValue<string>().Split('/')[^1]);

        factory.Storage.Blobs.Should().NotContainKey(idPrimeira);
        factory.Storage.Blobs.Should().ContainKey(idSegunda);
        factory.Storage.Blobs.Count.Should().Be(blobsAntes + 1);
        (await usuario.Http.GetAsync(urlPrimeira)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await usuario.Http.GetAsync($"/api/arquivos/{idSegunda}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await usuario.Http.DeleteAsync("/api/usuarios/me/foto")).StatusCode.Should().Be(HttpStatusCode.OK);
        factory.Storage.Blobs.Should().NotContainKey(idSegunda);
    }

    [Fact]
    public async Task Upload_recusa_arquivo_que_nao_e_imagem()
    {
        var usuario = await factory.CriarUsuarioAsync("fake");
        var albumId = await usuario.CriarAlbumAsync();
        var antes = factory.Storage.Blobs.Count;

        var resposta = await usuario.Http.PostAsync($"/api/albuns/{albumId}/fotos",
            ApiClientExtensions.Multipart(("arquivos", "malware.png", "MZ nao sou imagem"u8.ToArray())));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.Storage.Blobs.Count.Should().Be(antes);
    }

    [Fact]
    public async Task Excluir_album_remove_as_fotos_do_storage()
    {
        var dono = await factory.CriarUsuarioAsync("del");
        var albumId = await dono.CriarAlbumAsync();
        var ids = (await (await dono.Http.PostAsync($"/api/albuns/{albumId}/fotos", ApiClientExtensions.Imagem("arquivos", 3))).LerAsync())
            .AsArray().Select(f => Guid.Parse(f!["arquivoId"]!.GetValue<string>())).ToList();
        factory.Storage.Blobs.Keys.Should().Contain(ids);

        (await dono.Http.DeleteAsync($"/api/albuns/{albumId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        factory.Storage.Blobs.Keys.Should().NotContain(ids);
        (await dono.Http.GetAsync($"/api/albuns/{albumId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rotas_publicas_aplicam_rate_limit_e_devolvem_retry_after()
    {
        using var restrita = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:Auth:PermitLimit"] = "3" })));
        var cliente = restrita.CreateClient();

        var codigos = new List<HttpStatusCode>();
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 5; i++)
        {
            ultima = await cliente.PostAsJsonAsync("/api/auth/login", new { email = "alguem@teste.com", senha = "Errada@123" });
            codigos.Add(ultima.StatusCode);
        }

        codigos.Take(3).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        codigos.Skip(3).Should().AllBeEquivalentTo(HttpStatusCode.TooManyRequests);
        ultima!.Headers.RetryAfter.Should().NotBeNull();
    }
}
