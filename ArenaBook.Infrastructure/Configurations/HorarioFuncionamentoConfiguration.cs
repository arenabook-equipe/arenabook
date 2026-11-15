using ArenaBook.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class HorarioFuncionamentoConfiguration : IEntityTypeConfiguration<HorarioFuncionamento>
{
    public void Configure(EntityTypeBuilder<HorarioFuncionamento> builder)
    {
        builder.ToTable("HorarioFuncionamento", table =>
        {
            table.HasCheckConstraint("CK_Horario_DiaSemana", "DiaSemana BETWEEN 1 AND 7");
            table.HasCheckConstraint("CK_Horario_Intervalo", "HoraFechamento > HoraAbertura");
        });
        builder.HasKey(e => e.IdHorario);
        builder.Property(e => e.IdHorario).UseIdentityColumn();
        builder.Property(e => e.DiaSemana).HasColumnType("tinyint");
        builder.Property(e => e.HoraAbertura).HasColumnType("time(0)");
        builder.Property(e => e.HoraFechamento).HasColumnType("time(0)");
        builder.HasAlternateKey(e => new { e.IdQuadra, e.DiaSemana, e.HoraAbertura }).HasName("UQ_Horario_Inicio");
        builder.HasOne(e => e.Quadra).WithMany(e => e.HorariosFuncionamento).HasForeignKey(e => e.IdQuadra)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Horario_Quadra");
    }
}

