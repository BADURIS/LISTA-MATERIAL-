using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materiais");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(m => m.Nome)
            .IsUnique();

        builder.Property(m => m.Unidade)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.OrdemExibicao)
            .IsRequired();

        builder.Property(m => m.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        // Seed de Dados Estáticos (31 Materiais)
        builder.HasData(
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Copo Plástico", Unidade.UN, 1),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Papel Toalha", Unidade.CX, 2),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Papel Higiênico", Unidade.CX, 3),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Sabonete", Unidade.UN, 4),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000005"), "Tela Mictório", Unidade.UN, 5),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000006"), "Protetor Assento", Unidade.UN, 6),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000007"), "Detergente", Unidade.UN, 7),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000008"), "Esponja de Pia", Unidade.UN, 8),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000009"), "Fibra Branca", Unidade.UN, 9),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000010"), "Fibra Verde", Unidade.UN, 10),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000011"), "Fio Dental", Unidade.UN, 11),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000012"), "Álcool em Gel", Unidade.UN, 12),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000013"), "Saco de Marmita", Unidade.UN, 13),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000014"), "Inseticida", Unidade.UN, 14),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000015"), "Sabão Barra", Unidade.UN, 15),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000016"), "Cotonete", Unidade.CX, 16),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000017"), "Descarte Absorvente", Unidade.UN, 17),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000018"), "Saco de Lixo Azul 20L", Unidade.PCT, 18),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000019"), "Saco de Lixo Azul 40L", Unidade.PCT, 19),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000020"), "Saco de Lixo Azul 60L", Unidade.PCT, 20),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000021"), "Saco de Lixo Azul 100L", Unidade.PCT, 21),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000022"), "Saco de Lixo Vermelho 20L", Unidade.PCT, 22),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000023"), "Saco de Lixo Vermelho 40L", Unidade.PCT, 23),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000024"), "Saco de Lixo Vermelho 60L", Unidade.PCT, 24),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000025"), "Saco de Lixo Vermelho 100L", Unidade.PCT, 25),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000026"), "Saco de Lixo Preto 20L", Unidade.PCT, 26),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000028"), "Saco de Lixo Preto 60L", Unidade.PCT, 28),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000029"), "Saco de Lixo Preto 100L", Unidade.PCT, 29),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000030"), "Saco de Lixo Preto 200L", Unidade.PCT, 30),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000031"), "Saco de Lixo Amarelo 100L", Unidade.PCT, 31),
            Material.CreateWithId(Guid.Parse("10000000-0000-0000-0000-000000000032"), "Saco de Lixo Amarelo 60L", Unidade.PCT, 32)
        );
    }
}
