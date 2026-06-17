using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SistemaManutencao.API.Models;

public enum StatusOS
{
    AguardandoEquipamento = 1,
    EquipamentoRecebido = 2,
    EmAnalise = 3,
    AguardandoAprovacao = 4,
    EmManutencao = 5,
    AguardandoPecas = 6,
    Finalizado = 7,
    EntregueAoCliente = 8
}

public class OrdemServico
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("numero")]
    public string Numero { get; set; } = string.Empty;  // OS-2024-0001

    [BsonElement("clienteId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ClienteId { get; set; } = string.Empty;

    [BsonElement("equipamentoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EquipamentoId { get; set; } = string.Empty;

    [BsonElement("tecnicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? TecnicoId { get; set; }

    [BsonElement("descricaoProblema")]
    public string DescricaoProblema { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public StatusOS Status { get; set; } = StatusOS.AguardandoEquipamento;

    [BsonElement("valorTotal")]
    public decimal ValorTotal { get; set; }

    [BsonElement("previsaoConclusao")]
    public DateTime? PrevisaoConclusao { get; set; }

    [BsonElement("dataAbertura")]
    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;

    [BsonElement("dataFinalizacao")]
    public DateTime? DataFinalizacao { get; set; }

    [BsonElement("pecasUtilizadas")]
    public List<PecaUtilizada> PecasUtilizadas { get; set; } = new();

    [BsonElement("observacoes")]
    public string Observacoes { get; set; } = string.Empty;

    [BsonElement("tipoEntrega")]
    public string TipoEntrega { get; set; } = "Balcao";  // Balcao, Transportadora

    // State machine: validação de transições permitidas
    private static readonly Dictionary<StatusOS, List<StatusOS>> _transicoesPermitidas = new()
    {
        { StatusOS.AguardandoEquipamento, new() { StatusOS.EquipamentoRecebido } },
        { StatusOS.EquipamentoRecebido,   new() { StatusOS.EmAnalise } },
        { StatusOS.EmAnalise,             new() { StatusOS.AguardandoAprovacao } },
        { StatusOS.AguardandoAprovacao,   new() { StatusOS.EmManutencao, StatusOS.Finalizado } },
        { StatusOS.EmManutencao,          new() { StatusOS.AguardandoPecas, StatusOS.Finalizado } },
        { StatusOS.AguardandoPecas,       new() { StatusOS.EmManutencao } },
        { StatusOS.Finalizado,            new() { StatusOS.EntregueAoCliente } },
        { StatusOS.EntregueAoCliente,     new() { } }
    };

    public bool PodeTransicionarPara(StatusOS novoStatus)
    {
        return _transicoesPermitidas.TryGetValue(Status, out var permitidos)
            && permitidos.Contains(novoStatus);
    }

    public void TransicionarPara(StatusOS novoStatus)
    {
        if (!PodeTransicionarPara(novoStatus))
            throw new InvalidOperationException(
                $"Transição inválida: {Status} → {novoStatus}.");

        Status = novoStatus;

        if (novoStatus is StatusOS.Finalizado or StatusOS.EntregueAoCliente)
            DataFinalizacao ??= DateTime.UtcNow;
    }
}

public class PecaUtilizada
{
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}
