using ArenaBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class QuadraConfiguration : IEntityTypeConfiguration<Quadra>
{
    public void Configure(EntityTypeBuilder<Quadra> builder)
    {
        builder.ToTable("Quadra", table => table.HasCheckConstraint("CK_Quadra_Preco", "PrecoHora > 0"));
        builder.HasKey(e => e.IdQuadra);
        builder.Property(e => e.IdQuadra).UseIdentityColumn();
        builder.Property(e => e.Nome).HasColumnType("nvarchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.PrecoHora).HasColumnType("decimal(10,2)");
        builder.Property(e => e.Ativa).HasDefaultValue(true).HasSentinel(true);
    }
}

