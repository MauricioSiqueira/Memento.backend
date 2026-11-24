using Memento.Api.Extensions;
using Memento.Api.Options;
using Memento.Application;
using Memento.Application.Arquivos;
using Memento.Infrastructure;
using Memento.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.Secao));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.Secao));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAutenticacaoJwt();
builder.Services.AddLimitacaoDeRequisicoes();

var origensCors = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origensCors.Length > 0) p.WithOrigins(origensCors).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddSwaggerComJwt();

var app = builder.Build();

app.UseExceptionHandler();

app.Use(async (contexto, next) =>
{
    var h = contexto.Response.Headers;
    h.XContentTypeOptions = "nosniff";
    h.XFrameOptions = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Conveniência de desenvolvimento: aplica as migrations pendentes ao subir.
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter(); // depois da autenticação: o limite geral usa o usuário como chave
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory<Program> nos testes de integração.
public partial class Program;
