using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DllTeste.Banco.Models;

/// <summary>
/// Configuração fluente da entidade Usuario.
/// Opcional: use se o seu DbContext aplica IEntityTypeConfiguration.
/// (Se você configura tudo via data annotations / OnModelCreating manual,
///  pode ignorar este arquivo e mapear como já faz hoje.)
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {

        builder.HasKey(u => u.Id);

        builder.Property(u => u.NomeUsuario).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.NomeExibicao).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Ativo).IsRequired();

        builder.HasIndex(u => u.NomeUsuario).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

// No seu DbContext concreto (o que é resolvido via ConexaoCentralizada):
//
//   public DbSet<Usuario> Usuarios => Set<Usuario>();
//
//   protected override void OnModelCreating(ModelBuilder modelBuilder)
//   {
//       base.OnModelCreating(modelBuilder);
//       modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
//   }
