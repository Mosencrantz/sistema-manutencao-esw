using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SistemaManutencao.API.Models;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(Cliente), typeof(Funcionario), typeof(Tecnico), typeof(Administrador))]
public abstract class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("senha")]
    public string Senha { get; set; } = string.Empty;

    [BsonElement("telefone")]
    public string Telefone { get; set; } = string.Empty;

    [BsonElement("perfil")]
    public string Perfil { get; set; } = string.Empty;

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;

    [BsonElement("criadoEm")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public void AlterarSenha(string senhaAtual, string novaSenha)
    {
        if (!BCrypt.Net.BCrypt.Verify(senhaAtual, Senha))
            throw new InvalidOperationException("Senha atual incorreta.");
        Senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);
    }
}

public class Cliente : Usuario
{
    [BsonElement("endereco")]
    public string Endereco { get; set; } = string.Empty;

    public Cliente() => Perfil = "Cliente";
}

public class Funcionario : Usuario
{
    public Funcionario() => Perfil = "Funcionario";
}

public class Tecnico : Usuario
{
    public Tecnico() => Perfil = "Tecnico";
}

public class Administrador : Usuario
{
    public Administrador() => Perfil = "Administrador";
}
