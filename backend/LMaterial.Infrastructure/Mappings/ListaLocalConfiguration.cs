using LMaterial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class ListaLocalConfiguration : IEntityTypeConfiguration<ListaLocal>
{
    public void Configure(EntityTypeBuilder<ListaLocal> builder)
    {
        builder.ToTable("ListasLocais");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(l => l.SeparadoPorAlmoxarifeId);
        builder.Property(l => l.DataSeparacao);

        builder.Property(l => l.NomeRetiradoPor)
            .HasMaxLength(150);

        builder.Property(l => l.DataRetirada);

        // Assinatura digital da retirada (nome + data/hora + responsável pelo registro)
        builder.Property(l => l.RegistradoPorUsuarioId);

        builder.Property(l => l.AssinaturaDigital)
            .HasMaxLength(100);

        builder.Property(l => l.SemPedido)
            .IsRequired()
            .HasDefaultValue(false);

        // Relacionamento com Local (Muitas listas podem referenciar o mesmo local, mas não há navegação inversa obrigatória no Local)
        builder.HasOne(l => l.Local)
            .WithMany()
            .HasForeignKey(l => l.LocalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento 1:N com itens de lista (ListaLocalItem) configurando o backing field privado
        builder.HasMany(l => l.Itens)
            .WithOne()
            .HasForeignKey(i => i.ListaLocalId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Índice composto exclusivo para garantir que exista apenas uma lista por local na mesma semana
        builder.HasIndex(l => new { l.SemanaListaId, l.LocalId })
            .IsUnique();
    }
}
