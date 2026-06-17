using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SistemaManutencao.API.Models;

public class Diagnostico
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("ordemServicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OrdemServicoId { get; set; } = string.Empty;

    [BsonElement("tecnicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TecnicoId { get; set; } = string.Empty;

    [BsonElement("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [BsonElement("observacoes")]
    public string Observacoes { get; set; } = string.Empty;

    [BsonElement("dataRegistro")]
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
}

public class Arquivo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("ordemServicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OrdemServicoId { get; set; } = string.Empty;

    [BsonElement("nomeArquivo")]
    public string NomeArquivo { get; set; } = string.Empty;

    [BsonElement("tipoArquivo")]
    public string TipoArquivo { get; set; } = string.Empty;  // image/jpeg, video/mp4

    [BsonElement("caminho")]
    public string Caminho { get; set; } = string.Empty;  // path no servidor

    [BsonElement("tamanhoBytes")]
    public long TamanhoBytes { get; set; }

    [BsonElement("dataUpload")]
    public DateTime DataUpload { get; set; } = DateTime.UtcNow;

    [BsonElement("uploadadoPor")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadadoPorId { get; set; } = string.Empty;
}

public class HistoricoOS
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("ordemServicoId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OrdemServicoId { get; set; } = string.Empty;

    [BsonElement("usuarioId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    [BsonElement("descricaoAlteracao")]
    public string DescricaoAlteracao { get; set; } = string.Empty;

    [BsonElement("statusAnterior")]
    [BsonRepresentation(BsonType.String)]
    public StatusOS? StatusAnterior { get; set; }

    [BsonElement("statusNovo")]
    [BsonRepresentation(BsonType.String)]
    public StatusOS? StatusNovo { get; set; }

    [BsonElement("dataAlteracao")]
    public DateTime DataAlteracao { get; set; } = DateTime.UtcNow;
}
