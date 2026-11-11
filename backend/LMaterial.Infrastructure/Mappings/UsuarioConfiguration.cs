using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(60);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.NomeCompleto)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.SenhaHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Perfil)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(u => u.Ativo)
            .IsRequired();

        builder.Property(u => u.RefreshTokenHash)
            .HasMaxLength(200);

        builder.Property(u => u.RefreshTokenExpiracao);
    }
}
