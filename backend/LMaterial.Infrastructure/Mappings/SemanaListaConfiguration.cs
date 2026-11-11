using LMaterial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace LMaterial.Infrastructure.Mappings;

public class SemanaListaConfiguration : IEntityTypeConfiguration<SemanaLista>
{
    public void Configure(EntityTypeBuilder<SemanaLista> builder)
    {
        builder.ToTable("SemanasListas");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.DataInicio)
            .IsRequired();

        builder.Property(s => s.DataFim)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.CriadoPorSupervisorId)
            .IsRequired();

        builder.Property(s => s.DataCriacao)
            .IsRequired();


        // Configuração de acesso direto ao Backing Field privado para a coleção de listas locais
        builder.HasMany(s => s.ListasLocais)
            .WithOne()
            .HasForeignKey(l => l.SemanaListaId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
