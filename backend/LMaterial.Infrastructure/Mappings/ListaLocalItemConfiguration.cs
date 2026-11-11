using LMaterial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class ListaLocalItemConfiguration : IEntityTypeConfiguration<ListaLocalItem>
{
    public void Configure(EntityTypeBuilder<ListaLocalItem> builder)
    {
        builder.ToTable("ListaLocalItens");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.QuantidadeSolicitada)
            .IsRequired();

        builder.Property(i => i.QuantidadeSeparada);

        builder.Property(i => i.StatusItem)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Observacao)
            .HasMaxLength(500);

        // Relacionamento com o Material
        builder.HasOne(i => i.Material)
            .WithMany()
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice composto exclusivo para garantir que um mesmo material não apareça mais de uma vez na mesma lista local
        builder.HasIndex(i => new { i.ListaLocalId, i.MaterialId })
            .IsUnique();
    }
}
