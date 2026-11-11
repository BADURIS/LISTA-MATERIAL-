using Microsoft.EntityFrameworkCore;
using LMaterial.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMaterial.Infrastructure.Mappings;

public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("Notificacoes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Mensagem)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.DataCriacao)
            .IsRequired();

        builder.Property(n => n.Lida)
            .IsRequired();

        builder.HasIndex(n => new { n.UsuarioDestinoId, n.Lida });
    }
}
