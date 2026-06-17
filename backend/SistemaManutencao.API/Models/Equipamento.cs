using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SistemaManutencao.API.Models;

public class Equipamento
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("clienteId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ClienteId { get; set; } = string.Empty;

    [BsonElement("tipo")]
    public string Tipo { get; set; } = string.Empty;  // Desktop, Notebook, All-in-One, etc.

    [BsonElement("marca")]
    public string Marca { get; set; } = string.Empty;

    [BsonElement("modelo")]
    public string Modelo { get; set; } = string.Empty;

    [BsonElement("numeroSerie")]
    public string NumeroSerie { get; set; } = string.Empty;

    [BsonElement("descricaoProblema")]
    public string DescricaoProblema { get; set; } = string.Empty;

    [BsonElement("criadoEm")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;
}
