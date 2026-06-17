namespace SistemaManutencao.API.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "SistemaManutencao";
}

public class JwtSettings
{
    public string SecretKey { get; set; } = "SistemaManutencao_SuperSecretKey_2024!";
    public string Issuer { get; set; } = "SistemaManutencao";
    public string Audience { get; set; } = "SistemaManutencaoUsers";
    public int ExpirationHours { get; set; } = 8;
}
