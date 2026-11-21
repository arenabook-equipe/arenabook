using ArenaBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class EsporteConfiguration : IEntityTypeConfiguration<Esporte>
{
    public void Configure(EntityTypeBuilder<Esporte> builder)
    {
        builder.ToTable("Esporte");
        builder.HasKey(e => e.IdEsporte);
        builder.Property(e => e.IdEsporte).UseIdentityColumn();
        builder.Property(e => e.Nome).HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.HasAlternateKey(e => e.Nome);
    }
}

