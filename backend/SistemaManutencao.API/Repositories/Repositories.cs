using MongoDB.Driver;
using MongoDB.Bson;
using SistemaManutencao.API.Models;

namespace SistemaManutencao.API.Repositories;

// ─── Generic base ─────────────────────────────────────────────────────────────

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(string id, T entity);
    Task DeleteAsync(string id);
}

public class MongoRepository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task<List<T>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<T?> GetByIdAsync(string id)
    {
        // Usa ObjectId.Parse para garantir comparação correta de tipo
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<T> CreateAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(string id, T entity)
    {
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        await _collection.ReplaceOneAsync(filter, entity);
    }

    public async Task DeleteAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        await _collection.DeleteOneAsync(filter);
    }
}

// ─── Usuario Repository ───────────────────────────────────────────────────────

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email);
}

public class UsuarioRepository : MongoRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(IMongoDatabase db) : base(db, "usuarios") { }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        // email é string normal — filtro direto funciona
        var filter = Builders<Usuario>.Filter.Eq(u => u.Email, email);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}

// ─── Equipamento Repository ───────────────────────────────────────────────────

public interface IEquipamentoRepository : IRepository<Equipamento>
{
    Task<List<Equipamento>> GetByClienteIdAsync(string clienteId);
}

public class EquipamentoRepository : MongoRepository<Equipamento>, IEquipamentoRepository
{
    public EquipamentoRepository(IMongoDatabase db) : base(db, "equipamentos") { }

    public async Task<List<Equipamento>> GetByClienteIdAsync(string clienteId)
    {
        // CORREÇÃO: usa lambda tipado para que [BsonRepresentation(ObjectId)]
        // seja respeitado — filtro por string "clienteId" ignorava o tipo ObjectId
        var filter = Builders<Equipamento>.Filter.Eq(e => e.ClienteId, clienteId);
        return await _collection.Find(filter).ToListAsync();
    }
}

// ─── OrdemServico Repository ──────────────────────────────────────────────────

public interface IOrdemServicoRepository : IRepository<OrdemServico>
{
    Task<List<OrdemServico>> GetByClienteIdAsync(string clienteId);
    Task<List<OrdemServico>> GetByStatusAsync(StatusOS status);
    Task<List<OrdemServico>> GetByTecnicoIdAsync(string tecnicoId);
    Task<string> GerarNumeroOSAsync();
    Task<OrdemServico?> GetByNumeroAsync(string numero);
}

public class OrdemServicoRepository : MongoRepository<OrdemServico>, IOrdemServicoRepository
{
    public OrdemServicoRepository(IMongoDatabase db) : base(db, "ordens_servico") { }

    public async Task<List<OrdemServico>> GetByClienteIdAsync(string clienteId)
    {
        // CORREÇÃO: lambda tipado respeita [BsonRepresentation(ObjectId)]
        var filter = Builders<OrdemServico>.Filter.Eq(o => o.ClienteId, clienteId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<List<OrdemServico>> GetByStatusAsync(StatusOS status)
    {
        // status é enum serializado como string — lambda tipado aplica conversão correta
        var filter = Builders<OrdemServico>.Filter.Eq(o => o.Status, status);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<List<OrdemServico>> GetByTecnicoIdAsync(string tecnicoId)
    {
        var filter = Builders<OrdemServico>.Filter.Eq(o => o.TecnicoId, tecnicoId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<OrdemServico?> GetByNumeroAsync(string numero)
    {
        // numero é string simples — sem problema de tipo
        var filter = Builders<OrdemServico>.Filter.Eq(o => o.Numero, numero);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<string> GerarNumeroOSAsync()
    {
        var count = await _collection.CountDocumentsAsync(_ => true);
        return $"OS-{DateTime.UtcNow.Year}-{(count + 1):D4}";
    }
}

// ─── Diagnostico Repository ───────────────────────────────────────────────────

public interface IDiagnosticoRepository : IRepository<Diagnostico>
{
    Task<List<Diagnostico>> GetByOrdemServicoIdAsync(string osId);
}

public class DiagnosticoRepository : MongoRepository<Diagnostico>, IDiagnosticoRepository
{
    public DiagnosticoRepository(IMongoDatabase db) : base(db, "diagnosticos") { }

    public async Task<List<Diagnostico>> GetByOrdemServicoIdAsync(string osId)
    {
        var filter = Builders<Diagnostico>.Filter.Eq(d => d.OrdemServicoId, osId);
        return await _collection.Find(filter).ToListAsync();
    }
}

// ─── Arquivo Repository ───────────────────────────────────────────────────────

public interface IArquivoRepository : IRepository<Arquivo>
{
    Task<List<Arquivo>> GetByOrdemServicoIdAsync(string osId);
}

public class ArquivoRepository : MongoRepository<Arquivo>, IArquivoRepository
{
    public ArquivoRepository(IMongoDatabase db) : base(db, "arquivos") { }

    public async Task<List<Arquivo>> GetByOrdemServicoIdAsync(string osId)
    {
        var filter = Builders<Arquivo>.Filter.Eq(a => a.OrdemServicoId, osId);
        return await _collection.Find(filter).ToListAsync();
    }
}

// ─── HistoricoOS Repository ───────────────────────────────────────────────────

public interface IHistoricoOSRepository : IRepository<HistoricoOS>
{
    Task<List<HistoricoOS>> GetByOrdemServicoIdAsync(string osId);
}

public class HistoricoOSRepository : MongoRepository<HistoricoOS>, IHistoricoOSRepository
{
    public HistoricoOSRepository(IMongoDatabase db) : base(db, "historico_os") { }

    public async Task<List<HistoricoOS>> GetByOrdemServicoIdAsync(string osId)
    {
        var filter = Builders<HistoricoOS>.Filter.Eq(h => h.OrdemServicoId, osId);
        return await _collection
            .Find(filter)
            .SortByDescending(h => h.DataAlteracao)
            .ToListAsync();
    }
}
