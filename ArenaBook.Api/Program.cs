using ArenaBook.Api.Configuration;
using ArenaBook.Api.Diagnostics;
using ArenaBook.Core.Interfaces;
using ArenaBook.Core.Services;
using ArenaBook.Infrastructure.Repositories;

var checkDatabase = args.Contains("--check-database", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--check-database").ToArray());

if (checkDatabase)
{
    // O diagnostico emite somente resultados controlados, sem logs de conexao do provedor.
    builder.Logging.ClearProviders();
    if (!builder.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("--check-database só pode ser executado em Development.");
        Environment.ExitCode = 1;
        return;
    }
}
builder.Services.AddArenaBookDatabase(builder.Configuration);
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ClienteService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    //documenta os inteiros como numeros para compatibilidade com o Swagger.
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(int))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.Integer;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    });

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "ArenaBook API";
        document.Info.Version = "v1";
        document.Info.Description = """
            API de agendamento de quadras.

            ### Criar reserva
            POST /api/reservas

            Exemplo com dados fictícios (substitua pelos identificadores cadastrados):
            ```json
            {
              "idQuadra": 1,
              "idEsporte": 1,
              "idCliente": 1,
              "dataReserva": "2030-01-06",
              "horaInicio": "18:00:00",
              "horaFim": "20:00:00"
            }
            ```

            Com preço de R$ 80 por hora, a duração de duas horas resulta em R$ 160.
            O identificador é gerado pelo banco e o status inicial é Pendente.

            ### Consultar agenda
            GET /api/reservas/agenda?idQuadra=1&data=2030-01-06

            Retorna as reservas Pendente e Confirmada em ordem de início.
            Sem ocupação, retorna uma lista vazia: [].

            ### Cancelar reserva
            PATCH /api/reservas/1/cancelar

            Não exige corpo. Retorna a reserva com status Cancelada, mantendo o histórico.

            ### Respostas de criação e cancelamento
            - 400: campos, identificadores ou horários inválidos.
            - 404: cliente, quadra ou reserva inexistente.
            - 409: sobreposição ou recusa pelas condições de negócio.
            - 500: falha técnica inesperada.

            Erros tratados pelo Controller usam o campo mensagem.
            Erros automáticos de formato e validação usam ValidationProblemDetails.
            """;

        return Task.CompletedTask;
    });
});
builder.Services.AddScoped<IQuadraCadastroRepository, QuadraCadastroRepository>();
builder.Services.AddScoped<QuadraService>();
builder.Services.AddScoped<IQuadraDisponibilidade>(sp => sp.GetRequiredService<QuadraService>());

builder.Services.AddScoped<IQuadraRepository, QuadraRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();

builder.Services.AddScoped<IHorarioFuncionamentoRepository, HorarioFuncionamentoRepository>();
builder.Services.AddScoped<HorarioFuncionamentoService>();

var app = builder.Build();

if (checkDatabase)
{
    Environment.ExitCode = await DatabaseConnectionCheck.RunAsync(app.Services);
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ArenaBook API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

