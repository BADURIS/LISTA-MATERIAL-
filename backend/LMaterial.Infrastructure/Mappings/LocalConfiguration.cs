using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class LocalConfiguration : IEntityTypeConfiguration<Local>
{
    public void Configure(EntityTypeBuilder<Local> builder)
    {
        builder.ToTable("Locais");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(l => l.Nome)
            .IsUnique();

        builder.Property(l => l.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.OrdemExibicao)
            .IsRequired();

        builder.Property(l => l.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        // Seed de Dados Estáticos (23 Locais)
        builder.HasData(
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Andar 1", TipoLocal.Andar, 1, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Andar 2", TipoLocal.Andar, 2, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Andar 3", TipoLocal.Andar, 3, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000004"), "Andar 4", TipoLocal.Andar, 4, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000005"), "Andar 5", TipoLocal.Andar, 5, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000006"), "Andar 6", TipoLocal.Andar, 6, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000007"), "Andar 7", TipoLocal.Andar, 7, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Andar 8", TipoLocal.Andar, 8, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Andar 9", TipoLocal.Andar, 9, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000010"), "Andar 10", TipoLocal.Andar, 10, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000011"), "Andar 11", TipoLocal.Andar, 11, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000012"), "Andar 12", TipoLocal.Andar, 12, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000013"), "Andar 13", TipoLocal.Andar, 13, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000014"), "Andar 14", TipoLocal.Andar, 14, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000015"), "Andar 15", TipoLocal.Andar, 15, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000016"), "Andar 16", TipoLocal.Andar, 16, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000017"), "Recepção", TipoLocal.AreaComum, 17, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000018"), "COI1", TipoLocal.AreaComum, 18, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000019"), "COI2", TipoLocal.AreaComum, 19, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000020"), "Docas", TipoLocal.AreaComum, 20, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000021"), "Refeitório", TipoLocal.AreaComum, 21, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000022"), "Estoque", TipoLocal.AreaComum, 22, true),
            Local.CreateWithId(Guid.Parse("20000000-0000-0000-0000-000000000023"), "Ambulatório", TipoLocal.AreaComum, 23, true)
        );
    }
}
