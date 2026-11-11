using LMaterial.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMaterial.Infrastructure.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Material> Materiais => Set<Material>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<SemanaLista> SemanasListas => Set<SemanaLista>();
    public DbSet<ListaLocal> ListasLocais => Set<ListaLocal>();
    public DbSet<ListaLocalItem> ListaLocalItens => Set<ListaLocalItem>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas as configurações Fluent API definidas no mesmo assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
