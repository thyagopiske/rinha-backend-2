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

// string UnixSocketPath = builder.Configuration["UnixSocketPath"];

// if (File.Exists(UnixSocketPath))
// {
//     File.Delete(UnixSocketPath);
// }


// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenUnixSocket(UnixSocketPath);
// });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapPost("/clientes/{id}/transacoes", async (
    int id, 
    NpgsqlService npgsqlService,
    TransacaoRequestDto transacao) =>
{
    if (!int.TryParse(transacao.Valor?.ToString(), out var valorInt))
    {
        return Results.UnprocessableEntity();
    }

    if (
        valorInt < 0
        || transacao.Descricao?.Length > 10
        || (transacao.Tipo != 'c' && transacao.Tipo != 'd')
        || transacao.Descricao is null || transacao?.Descricao == ""
    )
    {
        return Results.UnprocessableEntity();
    }

    await using var conn = await npgsqlService.dataSource.OpenConnectionAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = "select * from criarTransacao(@clienteId, @valor, @tipo, @descricao)";

    cmd.Parameters.AddWithValue("clienteId", id);
    cmd.Parameters.AddWithValue("valor", valorInt);
    cmd.Parameters.AddWithValue("tipo", transacao.Tipo);
    cmd.Parameters.AddWithValue("descricao", transacao.Descricao);

    await cmd.PrepareAsync();

    await using var reader = await cmd.ExecuteReaderAsync();

    int? codigo = null, limite = null, saldo = null;
    if (await reader.ReadAsync())
    {
        codigo = await reader.GetFieldValueAsync<int?>(0);
        limite = await reader.GetFieldValueAsync<int?>(1);
        saldo = await reader.GetFieldValueAsync<int?>(2);
    } 


    if (codigo == -1)
    {
        return Results.NotFound();
    }

    if (codigo == -2)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Ok(new
    {   
        Limite = limite,
        Saldo = saldo
    });
});

app.MapGet("clientes/{id}/extrato", async (int id, NpgsqlService npgsqlService) =>
{

    ExtratoResponseDto extrato = null;

    await using var conn = await npgsqlService.dataSource.OpenConnectionAsync();
    var result = await conn.QueryFirstOrDefaultAsync<string>(@"
    select * from obterextrato(@idCliente)",
    new { idCliente = id }
    );

    extrato = JsonSerializer.Deserialize<ExtratoResponseDto>(result, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (extrato.Codigo == -1)
    {
        return Results.NotFound();
    }

    return Results.Ok(extrato);

});

app.Run();