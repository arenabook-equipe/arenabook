using ArenaBook.Core.Entities;
using ArenaBook.Core.Enums;
using ArenaBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ArenaBook.Test.Unit;

public class DatabaseMappingTests
{
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<ArenaBookDbContext>()
            .UseSqlServer("Server=localhost;Database=MappingOnly;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        using var context = new ArenaBookDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Test]
    public void Model_PreservesTablesKeysAndForeignKeys()
    {
        var model = CreateModel();
        Assert.That(model.GetEntityTypes().Select(e => e.GetTableName()),
            Is.EquivalentTo(new[] { "Clientes", "Quadra", "Esporte", "Quadra_Esporte", "HorarioFuncionamento", "Reserva" }));
        var join = model.FindEntityType(typeof(QuadraEsporte))!;
        Assert.That(join.FindPrimaryKey()!.Properties.Select(p => p.Name), Is.EqualTo(new[] { "IdQuadra", "IdEsporte" }));
        var reserva = model.FindEntityType(typeof(Reserva))!;
        Assert.That(reserva.GetForeignKeys().Count(), Is.EqualTo(2));
        Assert.That(reserva.GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(QuadraEsporte))
            .Properties.Select(p => p.Name), Is.EqualTo(new[] { "IdQuadra", "IdEsporte" }));
        Assert.That(model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).All(f => f.DeleteBehavior == DeleteBehavior.NoAction), Is.True);
        Assert.That(model.GetEntityTypes().SelectMany(e => e.GetProperties()).Any(p => p.IsShadowProperty()), Is.False);
    }

    [Test]
    public void Model_PreservesSqlTypesDefaultsChecksAndIndexes()
    {
        var model = CreateModel();
        var reserva = model.FindEntityType(typeof(Reserva))!;
        Assert.That(reserva.FindProperty("DataReserva")!.GetColumnType(), Is.EqualTo("date"));
        foreach (var property in new[] { "HoraInicio", "HoraFim" })
            Assert.That(reserva.FindProperty(property)!.GetColumnType(), Is.EqualTo("time(0)"));
        Assert.That(reserva.FindProperty("DataCriacao")!.GetColumnType(), Is.EqualTo("datetime2(0)"));
        Assert.That(reserva.FindProperty("DataCriacao")!.GetDefaultValueSql(), Is.EqualTo("SYSUTCDATETIME()"));
        Assert.That(reserva.FindProperty("ValorTotal")!.GetColumnType(), Is.EqualTo("decimal(10,2)"));
        var status = reserva.FindProperty("IdStatus")!;
        Assert.That(status.GetTypeMapping().Converter!.ConvertToProvider(StatusReserva.Concluida), Is.EqualTo(4));
        Assert.That(status.GetDefaultValue(), Is.EqualTo(StatusReserva.Pendente));
        Assert.That(Enum.GetValues<StatusReserva>().Select(s => (int)s), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(reserva.GetCheckConstraints().Select(c => c.Name), Is.EquivalentTo(new[] { "CK_Reserva_Horario", "CK_Reserva_Valor", "CK_Reserva_Status" }));
        Assert.That(model.GetEntityTypes().SelectMany(e => e.GetIndexes()).Count(), Is.EqualTo(2));
        var index = reserva.GetIndexes().Single(i => i.GetDatabaseName() == "IX_Reserva_Quadra_Data");
        Assert.That(index.Properties.Select(p => p.Name), Is.EqualTo(new[] { "IdQuadra", "DataReserva", "IdStatus" }));
        Assert.That(index.GetIncludeProperties(), Is.EqualTo(new[] { "HoraInicio", "HoraFim" }));
        Assert.That(reserva.GetIndexes().Single(i => i.GetDatabaseName() == "IX_Reserva_Cliente")
            .Properties.Select(p => p.Name), Is.EqualTo(new[] { "IdCliente", "DataReserva" }));
        var cliente = model.FindEntityType(typeof(Cliente))!;
        Assert.That(cliente.FindProperty("Email")!.GetColumnType(), Is.EqualTo("varchar(150)"));
        Assert.That(cliente.GetKeys().Any(k => k.Properties.Select(p => p.Name).SequenceEqual(new[] { "Email" })), Is.True);
        var horario = model.FindEntityType(typeof(HorarioFuncionamento))!;
        Assert.That(horario.FindProperty("DiaSemana")!.GetColumnType(), Is.EqualTo("tinyint"));
        Assert.That(horario.FindProperty("HoraAbertura")!.GetColumnType(), Is.EqualTo("time(0)"));
        Assert.That(horario.FindProperty("HoraFechamento")!.GetColumnType(), Is.EqualTo("time(0)"));
        Assert.That(horario.GetKeys().Single(k => !k.IsPrimaryKey()).Properties.Select(p => p.Name),
            Is.EqualTo(new[] { "IdQuadra", "DiaSemana", "HoraAbertura" }));
        Assert.That(model.FindEntityType(typeof(Quadra))!.FindProperty("Ativa")!.GetDefaultValue(), Is.EqualTo(true));
    }
}

