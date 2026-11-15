using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("Reserva", table =>
        {
            table.HasCheckConstraint("CK_Reserva_Horario", "HoraFim > HoraInicio");
            table.HasCheckConstraint("CK_Reserva_Valor", "ValorTotal >= 0");
            table.HasCheckConstraint("CK_Reserva_Status", "IdStatus IN (1, 2, 3, 4)");
        });
        builder.HasKey(e => e.IdReserva);
        builder.Property(e => e.IdReserva).UseIdentityColumn();
        builder.Property(e => e.DataReserva).HasColumnType("date");
        builder.Property(e => e.HoraInicio).HasColumnType("time(0)");
        builder.Property(e => e.HoraFim).HasColumnType("time(0)");
        builder.Property(e => e.ValorTotal).HasColumnType("decimal(10,2)");
        builder.Property(e => e.IdStatus).HasConversion<int>().HasColumnType("int").HasDefaultValue(StatusReserva.Pendente);
        builder.Property(e => e.DataCriacao).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(e => e.Cliente).WithMany(e => e.Reservas).HasForeignKey(e => e.IdCliente)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reserva_Cliente");
        builder.HasOne(e => e.QuadraEsporte).WithMany(e => e.Reservas).HasForeignKey(e => new { e.IdQuadra, e.IdEsporte })
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Reserva_QuadraEsporte");
        builder.HasIndex(e => new { e.IdQuadra, e.DataReserva, e.IdStatus }).HasDatabaseName("IX_Reserva_Quadra_Data")
            .IncludeProperties(e => new { e.HoraInicio, e.HoraFim });
        builder.HasIndex(e => new { e.IdCliente, e.DataReserva }).HasDatabaseName("IX_Reserva_Cliente");
    }
}

