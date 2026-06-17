using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Models;
using SistemaManutencao.API.Repositories;

namespace SistemaManutencao.API.Services;

public interface IOrdemServicoService
{
    Task<List<OrdemServicoResponseDto>> GetAllAsync();
    Task<OrdemServicoResponseDto> GetByIdAsync(string id);
    Task<OrdemServicoResponseDto> GetByNumeroAsync(string numero);
    Task<List<OrdemServicoResponseDto>> GetByClienteIdAsync(string clienteId);
    Task<List<OrdemServicoResponseDto>> GetByStatusAsync(string status);
    Task<OrdemServicoResponseDto> CreateAsync(OrdemServicoCreateDto dto, string usuarioId);
    Task<OrdemServicoResponseDto> AtualizarStatusAsync(string id, OrdemServicoUpdateStatusDto dto, string usuarioId);
    Task<OrdemServicoResponseDto> AdicionarPecaAsync(string id, PecaUtilizadaDto peca, string usuarioId);
    Task<OrdemServicoResponseDto> AtribuirTecnicoAsync(string id, string tecnicoId, string usuarioId);
    Task<List<HistoricoOS>> GetHistoricoAsync(string id);
}

public class OrdemServicoService : IOrdemServicoService
{
    private readonly IOrdemServicoRepository _osRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEquipamentoRepository _equipamentoRepo;
    private readonly IHistoricoOSRepository _historicoRepo;

    public OrdemServicoService(
        IOrdemServicoRepository osRepo,
        IUsuarioRepository usuarioRepo,
        IEquipamentoRepository equipamentoRepo,
        IHistoricoOSRepository historicoRepo)
    {
        _osRepo = osRepo;
        _usuarioRepo = usuarioRepo;
        _equipamentoRepo = equipamentoRepo;
        _historicoRepo = historicoRepo;
    }

    public async Task<List<OrdemServicoResponseDto>> GetAllAsync()
    {
        var ordens = await _osRepo.GetAllAsync();
        var dtos = new List<OrdemServicoResponseDto>();
        foreach (var os in ordens)
            dtos.Add(await MapToDtoAsync(os));
        return dtos;
    }

    public async Task<OrdemServicoResponseDto> GetByIdAsync(string id)
    {
        var os = await _osRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Ordem de serviço {id} não encontrada.");
        return await MapToDtoAsync(os);
    }

    public async Task<OrdemServicoResponseDto> GetByNumeroAsync(string numero)
    {
        var os = await _osRepo.GetByNumeroAsync(numero)
            ?? throw new KeyNotFoundException($"OS {numero} não encontrada.");
        return await MapToDtoAsync(os);
    }

    public async Task<List<OrdemServicoResponseDto>> GetByClienteIdAsync(string clienteId)
    {
        var ordens = await _osRepo.GetByClienteIdAsync(clienteId);
        var dtos = new List<OrdemServicoResponseDto>();
        foreach (var os in ordens)
            dtos.Add(await MapToDtoAsync(os));
        return dtos;
    }

    public async Task<List<OrdemServicoResponseDto>> GetByStatusAsync(string status)
    {
        if (!Enum.TryParse<StatusOS>(status, true, out var statusEnum))
            throw new ArgumentException($"Status inválido: {status}");

        var ordens = await _osRepo.GetByStatusAsync(statusEnum);
        var dtos = new List<OrdemServicoResponseDto>();
        foreach (var os in ordens)
            dtos.Add(await MapToDtoAsync(os));
        return dtos;
    }

    public async Task<OrdemServicoResponseDto> CreateAsync(OrdemServicoCreateDto dto, string usuarioId)
    {
        _ = await _usuarioRepo.GetByIdAsync(dto.ClienteId)
            ?? throw new KeyNotFoundException($"Cliente {dto.ClienteId} não encontrado.");

        _ = await _equipamentoRepo.GetByIdAsync(dto.EquipamentoId)
            ?? throw new KeyNotFoundException($"Equipamento {dto.EquipamentoId} não encontrado.");

        var numero = await _osRepo.GerarNumeroOSAsync();

        var os = new OrdemServico
        {
            Numero = numero,
            ClienteId = dto.ClienteId,
            EquipamentoId = dto.EquipamentoId,
            TecnicoId = dto.TecnicoId,
            DescricaoProblema = dto.DescricaoProblema,
            TipoEntrega = dto.TipoEntrega,
            PrevisaoConclusao = dto.PrevisaoConclusao
        };

        await _osRepo.CreateAsync(os);
        await RegistrarHistoricoAsync(os.Id, usuarioId, "Ordem de serviço criada.", null, os.Status);

        return await MapToDtoAsync(os);
    }

    public async Task<OrdemServicoResponseDto> AtualizarStatusAsync(
        string id, OrdemServicoUpdateStatusDto dto, string usuarioId)
    {
        var os = await _osRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"OS {id} não encontrada.");

        if (!Enum.TryParse<StatusOS>(dto.NovoStatus, true, out var novoStatus))
            throw new ArgumentException($"Status inválido: {dto.NovoStatus}");

        var statusAnterior = os.Status;
        os.TransicionarPara(novoStatus);

        if (dto.Observacao is not null)
            os.Observacoes += $"\n[{DateTime.UtcNow:dd/MM/yyyy HH:mm}] {dto.Observacao}";

        await _osRepo.UpdateAsync(id, os);
        await RegistrarHistoricoAsync(os.Id, usuarioId,
            dto.Observacao ?? $"Status alterado para {novoStatus}",
            statusAnterior, novoStatus);

        return await MapToDtoAsync(os);
    }

    public async Task<OrdemServicoResponseDto> AdicionarPecaAsync(
        string id, PecaUtilizadaDto peca, string usuarioId)
    {
        var os = await _osRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"OS {id} não encontrada.");

        os.PecasUtilizadas.Add(new PecaUtilizada
        {
            Nome = peca.Nome,
            Quantidade = peca.Quantidade,
            ValorUnitario = peca.ValorUnitario
        });

        os.ValorTotal = os.PecasUtilizadas.Sum(p => p.Quantidade * p.ValorUnitario);

        await _osRepo.UpdateAsync(id, os);
        await RegistrarHistoricoAsync(os.Id, usuarioId,
            $"Peça adicionada: {peca.Nome} (x{peca.Quantidade})");

        return await MapToDtoAsync(os);
    }

    public async Task<OrdemServicoResponseDto> AtribuirTecnicoAsync(
        string id, string tecnicoId, string usuarioId)
    {
        var os = await _osRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"OS {id} não encontrada.");

        var tecnico = await _usuarioRepo.GetByIdAsync(tecnicoId)
            ?? throw new KeyNotFoundException($"Técnico {tecnicoId} não encontrado.");

        os.TecnicoId = tecnicoId;
        await _osRepo.UpdateAsync(id, os);
        await RegistrarHistoricoAsync(os.Id, usuarioId,
            $"Técnico responsável: {tecnico.Nome}");

        return await MapToDtoAsync(os);
    }

    public async Task<List<HistoricoOS>> GetHistoricoAsync(string id) =>
        await _historicoRepo.GetByOrdemServicoIdAsync(id);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task RegistrarHistoricoAsync(string osId, string usuarioId,
        string descricao, StatusOS? anterior = null, StatusOS? novo = null)
    {
        await _historicoRepo.CreateAsync(new HistoricoOS
        {
            OrdemServicoId = osId,
            UsuarioId = usuarioId,
            DescricaoAlteracao = descricao,
            StatusAnterior = anterior,
            StatusNovo = novo
        });
    }

    private async Task<OrdemServicoResponseDto> MapToDtoAsync(OrdemServico os)
    {
        var cliente = await _usuarioRepo.GetByIdAsync(os.ClienteId);
        var equipamento = await _equipamentoRepo.GetByIdAsync(os.EquipamentoId);
        var tecnico = os.TecnicoId is not null
            ? await _usuarioRepo.GetByIdAsync(os.TecnicoId)
            : null;

        return new OrdemServicoResponseDto(
            os.Id, os.Numero,
            os.ClienteId, cliente?.Nome ?? "Desconhecido",
            os.EquipamentoId,
            equipamento is null ? "Desconhecido"
                : $"{equipamento.Marca} {equipamento.Modelo}",
            os.TecnicoId, tecnico?.Nome,
            os.DescricaoProblema,
            os.Status.ToString(),
            os.ValorTotal,
            os.PrevisaoConclusao,
            os.DataAbertura,
            os.DataFinalizacao,
            os.TipoEntrega,
            os.Observacoes,
            os.PecasUtilizadas.Select(p =>
                new PecaUtilizadaDto(p.Nome, p.Quantidade, p.ValorUnitario)).ToList());
    }
}
