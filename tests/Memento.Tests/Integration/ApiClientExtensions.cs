using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Memento.Tests.Integration;

/// <summary>Usuário de teste já autenticado (cada um com seu HttpClient).</summary>
public sealed class UsuarioTeste(HttpClient http, JsonObject login)
{
    public HttpClient Http { get; } = http;
    public string AccessToken => login["accessToken"]!.GetValue<string>();
    public string RefreshToken => login["refreshToken"]!.GetValue<string>();
    public Guid Id => Guid.Parse(login["usuario"]!["id"]!.GetValue<string>());
}

public static class ApiClientExtensions
{
    public const string Senha = "Senha@123";

    private static readonly byte[] PngMinimo = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public static async Task<UsuarioTeste> CriarUsuarioAsync(this ApiFactory factory, string prefixo = "u")
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefixo}.{sufixo}@teste.com";
        var cliente = factory.CreateClient();

        var cadastro = await cliente.PostAsJsonAsync("/api/auth/cadastro", new
        {
            nomeUsuario = $"{prefixo}{sufixo}",
            email,
            senha = Senha,
            confirmarSenha = Senha,
            dataNascimento = "1995-03-10"
        });
        cadastro.EnsureSuccessStatusCode();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = Senha });
        resposta.EnsureSuccessStatusCode();
        var login = (await resposta.Content.ReadFromJsonAsync<JsonObject>())!;

        var usuario = new UsuarioTeste(cliente, login);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", usuario.AccessToken);
        return usuario;
    }

    public static async Task<JsonNode> LerAsync(this HttpResponseMessage resposta)
        => (await resposta.Content.ReadFromJsonAsync<JsonNode>())!;

    public static MultipartFormDataContent Imagem(string campo, int variacao = 0)
    {
        var bytes = (byte[])PngMinimo.Clone();
        // Altera um byte depois do cabeçalho só para o hash do conteúdo variar entre as imagens.
        bytes[^1] = (byte)(bytes[^1] ^ variacao);
        return Multipart((campo, "foto.png", bytes));
    }

    public static MultipartFormDataContent Multipart(params (string Campo, string Nome, byte[] Bytes)[] partes)
    {
        var conteudo = new MultipartFormDataContent();
        foreach (var (campo, nome, bytes) in partes)
        {
            var parte = new ByteArrayContent(bytes);
            parte.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            conteudo.Add(parte, campo, nome);
        }
        return conteudo;
    }

    public static async Task<Guid> CriarAlbumAsync(this UsuarioTeste dono, string nome = "Viagem")
    {
        var r = await dono.Http.PostAsJsonAsync("/api/albuns", new { nome });
        r.EnsureSuccessStatusCode();
        return Guid.Parse((await r.LerAsync())["id"]!.GetValue<string>());
    }

    public static async Task<string> ObterLinkConviteAsync(this UsuarioTeste usuario, Guid albumId)
        => (await (await usuario.Http.GetAsync($"/api/albuns/{albumId}/convite")).LerAsync())["link"]!.GetValue<string>();
}
