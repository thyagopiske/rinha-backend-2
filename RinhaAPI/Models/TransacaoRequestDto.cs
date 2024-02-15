namespace RinhaAPI.Models;

public class TransacaoRequestDto
{
    public int ClienteId { get; set; }
    public object Valor { get; set; }
    public char Tipo { get; set; }
    public string Descricao { get; set; }
}
