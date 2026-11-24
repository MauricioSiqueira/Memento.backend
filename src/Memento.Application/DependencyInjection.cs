using FluentValidation;
using Memento.Application.Albuns;
using Memento.Application.Arquivos;
using Memento.Application.Auth;
using Memento.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Memento.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<AlbumService>();
        services.AddScoped<ArquivoService>();

        services.AddScoped<IValidator<CadastroRequest>, CadastroRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RefreshRequest>, RefreshRequestValidator>();
        services.AddScoped<IValidator<AtualizarPerfilRequest>, AtualizarPerfilRequestValidator>();
        services.AddScoped<IValidator<CriarAlbumRequest>, CriarAlbumRequestValidator>();
        services.AddScoped<IValidator<RenomearAlbumRequest>, RenomearAlbumRequestValidator>();

        return services;
    }
}
