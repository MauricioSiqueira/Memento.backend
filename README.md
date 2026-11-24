# Memento.backend

API do aplicativo Memento (criar álbuns de fotos e compartilhá-los por link/QR Code).

**Stack:** .NET 8 · ASP.NET Core · Entity Framework Core 8 · PostgreSQL · Azure Blob Storage (Azurite em desenvolvimento) · JWT Bearer.

## Arquitetura (Clean Architecture)

```
src/
  Memento.Domain/          entidades e regras de negócio (sem dependências)
  Memento.Application/     casos de uso (services), DTOs, validações, portas (interfaces)
  Memento.Infrastructure/  EF Core/Npgsql + migrations, repositórios, Azure Blob, JWT, hash de senha
  Memento.Api/             controllers, autenticação, rate limit, Program.cs, appsettings.json
tests/
  Memento.Tests/           unitários (domínio/aplicação) + integração (API real, Postgres local)
```

Dependências apontam para dentro: `Api → Application ← Infrastructure`, `Application → Domain`.
Os casos de uso devolvem `Resultado<T>` (sem exceções para erros esperados); a API traduz para HTTP/ProblemDetails.

## Como rodar

Pré-requisitos: .NET 8 SDK, PostgreSQL rodando em `localhost:5432` e Azurite.

```sh
# 1) Azurite (blob em 127.0.0.1:10000). --skipApiVersionCheck evita recusa de versões novas do SDK.
azurite --silent --skipApiVersionCheck --location ~/.azurite-memento

# 2) API — em Development aplica as migrations (criando o banco `memento`) ao subir.
cd Memento.backend
dotnet run --project src/Memento.Api --launch-profile http   # http://localhost:5077/swagger

# Testes (usam o banco `memento_tests`, recriado a cada execução, e storage em memória)
dotnet test
```

Configuração em `src/Memento.Api/appsettings.json` (não há `appsettings.Production.json` por enquanto):
`ConnectionStrings:Postgres` (usuário local sem senha), `Storage` (`UseDevelopmentStorage=true`),
`Jwt` (chave de **desenvolvimento**), `RateLimit`, `Upload`, `Cors`.

Migrations (tool local `dotnet-ef` 8.x): 
`dotnet ef migrations add <Nome> --project src/Memento.Infrastructure --startup-project src/Memento.Api --output-dir Persistence/Migrations`.

## Modelo de dados

| Tabela | Papel |
| --- | --- |
| `Usuarios` | e-mail e nome de usuário únicos (normalizados), senha PBKDF2, foto de perfil → `Arquivos` |
| `Sessoes` | uma por login/dispositivo; guarda só o **hash** do refresh token (+ o anterior, para detectar reuso) |
| `Albuns` | nome, dono, `TokenConvite` (link/QR) |
| `AlbunsUsuarios` | **ligação n-n** álbum↔usuário: papel (`Dono`/`Membro`) e `Favorito` (pessoal de cada um) |
| `Arquivos` | nome original, tamanho, content-type, SHA-256 e um **GUID gerado por nós = nome do blob** |
| `AlbunsArquivos` | liga as fotos ao álbum (quem enviou e quando) |

## Rotas

Tudo exige `Authorization: Bearer <access token>`, **exceto** `cadastro`, `login` e `refresh` (públicas, com rate limit estrito).
Qualquer rota nova é protegida por padrão (política de fallback).

| Método | Rota | Descrição |
| --- | --- | --- |
| POST | `/api/auth/cadastro` | cria usuário |
| POST | `/api/auth/login` | devolve access token (15 min) + refresh token (30 dias) |
| POST | `/api/auth/refresh` | rotaciona o refresh token e emite novo access token |
| POST | `/api/auth/logout` | revoga a sessão atual |
| GET/PUT | `/api/usuarios/me` | perfil / alterar nome de usuário |
| PUT/DELETE | `/api/usuarios/me/foto` | trocar (multipart, campo `arquivo`) / remover foto de perfil |
| GET/POST | `/api/albuns` | listar (com favorito do usuário) / criar |
| GET/PUT/DELETE | `/api/albuns/{id}` | detalhe / renomear (dono) / excluir (dono) |
| PUT | `/api/albuns/{id}/favorito` | `{ "favorito": true }` — por usuário |
| POST | `/api/albuns/{id}/fotos` | upload multipart (campo `arquivos`, várias) |
| DELETE | `/api/albuns/{id}/fotos/{arquivoId}` | dono remove qualquer; membro, só as suas |
| GET | `/api/albuns/{id}/membros` | participantes |
| DELETE | `/api/albuns/{id}/membros/{usuarioId}` | dono remove alguém / membro sai (o dono não sai) |
| GET | `/api/albuns/{id}/convite` | `{ "link": "memento://convite/{token}" }` — é o conteúdo do QR Code |
| POST | `/api/albuns/{id}/convite/regenerar` | (dono) invalida o link antigo |
| POST | `/api/albuns/entrar` | `{ "link": "..." }` — "escanear QR": entra no álbum como Membro (idempotente) |
| GET | `/api/arquivos/{id}` | stream do blob (container privado) para quem tem permissão |

## Decisões importantes

- **Sessão:** access token JWT curto + refresh token opaco com **rotação**. Reapresentar um refresh já rotacionado revoga a sessão (sinal de token roubado). Logout revoga a sessão; o access token já emitido segue válido até expirar (≤ 15 min).
- **Upload e blobs:** o app envia multipart à API, que valida (≤ 10 MB, JPEG/PNG/WebP/HEIC pelos *magic bytes*, não pelo content-type), grava o blob com o GUID do `Arquivo` e registra no banco. O container é privado.
- **Foto de perfil:** sobe o novo blob → numa única transação aponta o usuário para ele e remove o `Arquivo` antigo → só então apaga o blob antigo. Se algo falhar antes do commit, a foto atual continua intacta.
- **Consistência banco × blob:** em falha de commit os blobs recém-enviados são descartados; ao excluir, o banco vem primeiro e o blob depois (blob órfão é tolerável; registro sem blob não é).
- **Acesso a arquivos:** só quem enviou, membros de álbum que contém a foto, ou foto de perfil de si/de quem divide álbum. Sem permissão responde 404 (não revela existência).
- **Convite:** token permanente por álbum (32 bytes aleatórios); qualquer usuário logado com o link entra como Membro. O dono pode regenerá-lo.
- **Rate limit** (nativo do .NET 8, 429 + `Retry-After`): `auth` 5/min por IP e rota; `upload` 20/min por usuário; geral 100/min por usuário (ou IP).
- **Segurança:** senha PBKDF2; login com mensagem genérica e tempo uniforme (não revela se o e-mail existe); chave JWT, expiração e issuer/audience validados; headers `nosniff`/`DENY`; erros 500 não vazam detalhes.

## Pendências conhecidas

- A chave JWT em `appsettings.json` é só de desenvolvimento.
- Não há limpeza periódica de sessões expiradas nem de blobs órfãos.
- O frontend (`Memento/`) está integrado (auth, álbuns, fotos, convite/QR, perfil). Falta apenas Esqueci minha senha, que a API ainda não tem.
