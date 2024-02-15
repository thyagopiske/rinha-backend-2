using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Npgsql;
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
    var logger = loggerFactory.CreateLogger("meuLogger");

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
        || String.IsNullOrWhiteSpace(transacao.Descricao)
    )
    {
        return Results.UnprocessableEntity();
    }

    await using var conn2 = await npgsqlService.dataSource.OpenConnectionAsync();

    var obj = await conn2.QueryFirstOrDefaultAsync<MeuTipo>(@"
                             select * from criarTransacao(@clienteId, @valor, @tipo, @descricao)",
                             new
                             {
                                 clienteId = id,
                                 valor = int.Parse(transacao.Valor.ToString()),
                                 tipo = transacao.Tipo,
                                 descricao = transacao.Descricao
                             });

    if (obj.codigo == -1)
    {
        return Results.NotFound();
    }

    if (obj.codigo == -2)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Ok(new
    {   
        obj.limite,
        Saldo = obj.saldo
    });
});

app.MapGet("clientes/{id}/extrato", async (int id, NpgsqlService npgsqlService, IConfiguration config, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("loggerExtrato");

    ExtratoResponseDto extrato = null;
    try
    {
        await using var conn = await npgsqlService.dataSource.OpenConnectionAsync();
        var a = await conn.QueryFirstOrDefaultAsync<string>(@"
        select * from obterextrato(@idCliente)",
        new { idCliente = id }
        );

        extrato = JsonSerializer.Deserialize<ExtratoResponseDto>(a, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

    }
    catch (Exception)
    {
        logger.LogError("deu merda");
        throw;
    }

    var resp = extrato;

    if (resp.Codigo == -1)
    {
        return Results.NotFound();
    }

    return Results.Ok(resp);

});

bool IsConcurrencyException(PostgresException ex) => ex.SqlState == "40001";

app.Run();


public class Usuario()
{
    public int Id { get; set; }
    public string Nome { get; set; }
}
class Cliente()
{
    public int Id { get; set; }
    public int Limite { get; set; }
    public int Saldo { get; set; }
}


public class MeuTipo
{
    public int codigo { get; set; }
    public int limite { get; set; }
    public int saldo { get; set; }
};