using System.Text.Json.Serialization;

namespace RinhaAPI.Models;

public class ExtratoResponseDto
{
    public int Codigo { get; set; }
    public SaldoDto Saldo { get; set; }
    public List<TransacaoExtratoDto> UltimasTransacoes { get; set; } = new List<TransacaoExtratoDto>();
}

public class TransacaoExtratoDto
{
    public int Valor { get; set; }
    public char Tipo { get; set; }
    public string Descricao { get; set; }
    public DateTime RealizadaEm { get; set; }
}

public class SaldoDto
{
    public int Total { get; set; }
    public DateTime DataExtrato { get; set; }
    public int Limite { get; set; }
}