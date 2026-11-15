using ArenaBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class QuadraEsporteConfiguration : IEntityTypeConfiguration<QuadraEsporte>
{
    public void Configure(EntityTypeBuilder<QuadraEsporte> builder)
    {
        builder.ToTable("Quadra_Esporte");
        builder.HasKey(e => new { e.IdQuadra, e.IdEsporte }).HasName("PK_Quadra_Esporte");
        builder.HasOne(e => e.Quadra).WithMany(e => e.QuadraEsportes).HasForeignKey(e => e.IdQuadra)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_QuadraEsporte_Quadra");
        builder.HasOne(e => e.Esporte).WithMany(e => e.QuadraEsportes).HasForeignKey(e => e.IdEsporte)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_QuadraEsporte_Esporte");
    }
}

