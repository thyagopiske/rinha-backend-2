using Dapper;
using RinhaAPI.Models;
using RinhaAPI.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<NpgsqlService>();
builder.Services.ConfigureHttpJsonOptions(options => {
options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapPost("/clientes/{id}/transacoes", async (
    int id, 
    NpgsqlService npgsqlService,
    TransacaoRequestDto transacao, 
    ILoggerFactory loggerFactory,
    IConfiguration config) =>
{
    if (!int.TryParse(transacao.Valor?.ToString(), out var valorInt))
    {
        return Results.UnprocessableEntity();
    }

    if (valorInt < 0)
    {
        return Results.UnprocessableEntity();
    }

    if (
        transacao.Descricao?.Length > 10
        || (transacao.Tipo != 'c' && transacao.Tipo != 'd')
        || transacao.Descricao is null || transacao?.Descricao == ""
    )
    {
        return Results.UnprocessableEntity();
    }

    await using var conn2 = await npgsqlService.dataSource.OpenConnectionAsync();

    var obj = await conn2.QueryFirstOrDefaultAsync<TransacaoResponseDto>(@"
                             select * from criarTransacao(@clienteId, @valor, @tipo, @descricao)",
                             new
                             {
                                 clienteId = id,
                                 valor = valorInt,
                                 tipo = transacao.Tipo,
                                 descricao = transacao.Descricao
                             });

    if (obj.Codigo == -1)
    {
        return Results.NotFound();
    }

    if (obj.Codigo == -2)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Ok(new
    {   
        obj.Limite,
        Saldo = obj.Saldo
    });
});

app.MapGet("clientes/{id}/extrato", async (int id, NpgsqlService npgsqlService, IConfiguration config, ILoggerFactory loggerFactory) =>
{

    ExtratoResponseDto extrato = null;
    try
    {
        await using var conn = await npgsqlService.dataSource.OpenConnectionAsync();
        var result = await conn.QueryFirstOrDefaultAsync<string>(@"
        select * from obterextrato(@idCliente)",
        new { idCliente = id }
        );

        extrato = JsonSerializer.Deserialize<ExtratoResponseDto>(result, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    }
    catch (Exception)
    {
        throw;
    }


    if (extrato.Codigo == -1)
    {
        return Results.NotFound();
    }

    return Results.Ok(extrato);

});

app.Run();