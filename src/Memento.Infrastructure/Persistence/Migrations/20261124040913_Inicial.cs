using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memento.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DonoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenConvite = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlbunsArquivos",
                columns: table => new
                {
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArquivoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdicionadoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdicionadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbunsArquivos", x => new { x.AlbumId, x.ArquivoId });
                    table.ForeignKey(
                        name: "FK_AlbunsArquivos_Albuns_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbunsUsuarios",
                columns: table => new
                {
                    AlbumId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Papel = table.Column<int>(type: "integer", nullable: false),
                    Favorito = table.Column<bool>(type: "boolean", nullable: false),
                    EntrouEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbunsUsuarios", x => new { x.AlbumId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_AlbunsUsuarios_Albuns_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Arquivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Tamanho = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EnviadoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arquivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeUsuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NomeUsuarioNormalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    FotoPerfilArquivoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Arquivos_FotoPerfilArquivoId",
                        column: x => x.FotoPerfilArquivoId,
                        principalTable: "Arquivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RefreshTokenAnteriorHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevogadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Albuns_DonoId",
                table: "Albuns",
                column: "DonoId");

            migrationBuilder.CreateIndex(
                name: "IX_Albuns_TokenConvite",
                table: "Albuns",
                column: "TokenConvite",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlbunsArquivos_AdicionadoPorId",
                table: "AlbunsArquivos",
                column: "AdicionadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbunsArquivos_ArquivoId",
                table: "AlbunsArquivos",
                column: "ArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbunsUsuarios_UsuarioId",
                table: "AlbunsUsuarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Arquivos_EnviadoPorId",
                table: "Arquivos",
                column: "EnviadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_RefreshTokenAnteriorHash",
                table: "Sessoes",
                column: "RefreshTokenAnteriorHash");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_RefreshTokenHash",
                table: "Sessoes",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_UsuarioId",
                table: "Sessoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_FotoPerfilArquivoId",
                table: "Usuarios",
                column: "FotoPerfilArquivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NomeUsuarioNormalizado",
                table: "Usuarios",
                column: "NomeUsuarioNormalizado",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Albuns_Usuarios_DonoId",
                table: "Albuns",
                column: "DonoId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbunsArquivos_Arquivos_ArquivoId",
                table: "AlbunsArquivos",
                column: "ArquivoId",
                principalTable: "Arquivos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbunsArquivos_Usuarios_AdicionadoPorId",
                table: "AlbunsArquivos",
                column: "AdicionadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbunsUsuarios_Usuarios_UsuarioId",
                table: "AlbunsUsuarios",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Arquivos_Usuarios_EnviadoPorId",
                table: "Arquivos",
                column: "EnviadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arquivos_Usuarios_EnviadoPorId",
                table: "Arquivos");

            migrationBuilder.DropTable(
                name: "AlbunsArquivos");

            migrationBuilder.DropTable(
                name: "AlbunsUsuarios");

            migrationBuilder.DropTable(
                name: "Sessoes");

            migrationBuilder.DropTable(
                name: "Albuns");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Arquivos");
        }
    }
}
