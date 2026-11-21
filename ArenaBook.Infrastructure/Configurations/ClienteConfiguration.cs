using ArenaBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(e => e.IdCliente);
        builder.Property(e => e.IdCliente).UseIdentityColumn();
        builder.Property(e => e.Nome).HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Telefone).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(e => e.Email).HasColumnType("varchar(150)").HasMaxLength(150).IsUnicode(false).IsRequired();
        builder.HasAlternateKey(e => e.Email);
    }
}

