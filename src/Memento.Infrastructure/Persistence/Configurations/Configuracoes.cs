using Memento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Memento.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.NomeUsuario).HasMaxLength(50).IsRequired();
        b.Property(x => x.NomeUsuarioNormalizado).HasMaxLength(50).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.NomeUsuarioNormalizado).IsUnique();

        b.HasOne<Arquivo>()
            .WithMany()
            .HasForeignKey(x => x.FotoPerfilArquivoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class SessaoConfiguration : IEntityTypeConfiguration<Sessao>
{
    public void Configure(EntityTypeBuilder<Sessao> b)
    {
        b.ToTable("Sessoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.RefreshTokenAnteriorHash).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(300);
        b.Property(x => x.Ip).HasMaxLength(64);
        b.HasIndex(x => x.RefreshTokenHash).IsUnique();
        b.HasIndex(x => x.RefreshTokenAnteriorHash);
        b.HasIndex(x => x.UsuarioId);

        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> b)
    {
        b.ToTable("Albuns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(Album.TamanhoMaximoNome).IsRequired();
        b.Property(x => x.TokenConvite).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.TokenConvite).IsUnique();

        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.DonoId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Membros)
            .WithOne()
            .HasForeignKey(x => x.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Membros).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AlbumUsuarioConfiguration : IEntityTypeConfiguration<AlbumUsuario>
{
    public void Configure(EntityTypeBuilder<AlbumUsuario> b)
    {
        b.ToTable("AlbunsUsuarios");
        b.HasKey(x => new { x.AlbumId, x.UsuarioId });
        b.Property(x => x.Papel).HasConversion<int>();
        b.HasIndex(x => x.UsuarioId);

        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ArquivoConfiguration : IEntityTypeConfiguration<Arquivo>
{
    public void Configure(EntityTypeBuilder<Arquivo> b)
    {
        b.ToTable("Arquivos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.NomeOriginal).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Hash).HasMaxLength(64).IsRequired();

        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.EnviadoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AlbumArquivoConfiguration : IEntityTypeConfiguration<AlbumArquivo>
{
    public void Configure(EntityTypeBuilder<AlbumArquivo> b)
    {
        b.ToTable("AlbunsArquivos");
        b.HasKey(x => new { x.AlbumId, x.ArquivoId });
        b.HasIndex(x => x.ArquivoId);

        b.HasOne<Album>().WithMany().HasForeignKey(x => x.AlbumId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Arquivo).WithMany().HasForeignKey(x => x.ArquivoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AdicionadoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}
