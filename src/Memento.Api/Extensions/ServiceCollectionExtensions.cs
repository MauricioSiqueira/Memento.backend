using System.Text;
using System.Threading.RateLimiting;
using Memento.Api.Options;
using Memento.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Memento.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>JWT Bearer + política padrão: toda rota exige token, exceto as marcadas com [AllowAnonymous].</summary>
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Lido via IOptions (e não direto da configuração) para respeitar overrides aplicados depois do Program.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var o = jwt.Value;
                if (string.IsNullOrWhiteSpace(o.Key) || o.Key.Length < 32)
                    throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 caracteres.");

                bearer.MapInboundClaims = false; // mantém "sub" e "sid" com os nomes originais
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = o.Issuer,
                    ValidateAudience = true,
                    ValidAudience = o.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(o =>
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    public static IServiceCollection AddLimitacaoDeRequisicoes(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });

        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitOptions>>((limiter, config) =>
            {
                var cfg = config.Value;
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.OnRejected = async (contexto, ct) =>
                {
                    if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                        contexto.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString();

                    contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await contexto.HttpContext.Response.WriteAsJsonAsync(
                        new Microsoft.AspNetCore.Mvc.ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Muitas requisições. Tente novamente em instantes."
                        }, options: null, contentType: "application/problem+json", ct);
                };

                // Limite geral: por usuário autenticado (ou IP, para anônimos).
                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        $"api:{Identificar(http)}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = cfg.Api.PermitLimit,
                            Window = TimeSpan.FromSeconds(cfg.Api.WindowSeconds),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        }));

                // Rotas públicas sensíveis a força bruta: por IP e por rota.
                limiter.AddPolicy(PoliticasRateLimit.Auth, http =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        $"auth:{http.Connection.RemoteIpAddress}:{http.Request.Path}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = cfg.Auth.PermitLimit,
                            Window = TimeSpan.FromSeconds(cfg.Auth.WindowSeconds),
                            QueueLimit = 0
                        }));

                limiter.AddPolicy(PoliticasRateLimit.Upload, http =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        $"upload:{Identificar(http)}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = cfg.Upload.PermitLimit,
                            Window = TimeSpan.FromSeconds(cfg.Upload.WindowSeconds),
                            QueueLimit = 0
                        }));
            });

        return services;
    }

    public static IServiceCollection AddSwaggerComJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Memento API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe o access token JWT."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
        });
        return services;
    }

    private static string Identificar(HttpContext http)
        => http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}
