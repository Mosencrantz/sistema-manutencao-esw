using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Models;
using SistemaManutencao.API.Repositories;

namespace SistemaManutencao.API.Services;

public interface IEquipamentoService
{
    Task<List<EquipamentoResponseDto>> GetAllAsync();
    Task<EquipamentoResponseDto> GetByIdAsync(string id);
    Task<List<EquipamentoResponseDto>> GetByClienteIdAsync(string clienteId);
    Task<EquipamentoResponseDto> CreateAsync(EquipamentoCreateDto dto);
    Task<EquipamentoResponseDto> UpdateAsync(string id, EquipamentoUpdateDto dto);
    Task DeleteAsync(string id);
}

public class EquipamentoService : IEquipamentoService
{
    private readonly IEquipamentoRepository _repo;
    private readonly IUsuarioRepository _usuarioRepo;

    public EquipamentoService(IEquipamentoRepository repo, IUsuarioRepository usuarioRepo)
    {
        _repo = repo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<List<EquipamentoResponseDto>> GetAllAsync()
    {
        var equipamentos = await _repo.GetAllAsync();
        var dtos = new List<EquipamentoResponseDto>();
        foreach (var eq in equipamentos)
            dtos.Add(await MapToDtoAsync(eq));
        return dtos;
    }

    public async Task<EquipamentoResponseDto> GetByIdAsync(string id)
    {
        var eq = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Equipamento {id} não encontrado.");
        return await MapToDtoAsync(eq);
    }

    public async Task<List<EquipamentoResponseDto>> GetByClienteIdAsync(string clienteId)
    {
        var equipamentos = await _repo.GetByClienteIdAsync(clienteId);
        var dtos = new List<EquipamentoResponseDto>();
        foreach (var eq in equipamentos)
            dtos.Add(await MapToDtoAsync(eq));
        return dtos;
    }

    public async Task<EquipamentoResponseDto> CreateAsync(EquipamentoCreateDto dto)
    {
        var cliente = await _usuarioRepo.GetByIdAsync(dto.ClienteId)
            ?? throw new KeyNotFoundException($"Cliente {dto.ClienteId} não encontrado.");

        var equipamento = new Equipamento
        {
            ClienteId = dto.ClienteId,
            Tipo = dto.Tipo,
            Marca = dto.Marca,
            Modelo = dto.Modelo,
            NumeroSerie = dto.NumeroSerie,
            DescricaoProblema = dto.DescricaoProblema
        };

        await _repo.CreateAsync(equipamento);
        return await MapToDtoAsync(equipamento);
    }

    public async Task<EquipamentoResponseDto> UpdateAsync(string id, EquipamentoUpdateDto dto)
    {
        var eq = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Equipamento {id} não encontrado.");

        eq.Tipo = dto.Tipo;
        eq.Marca = dto.Marca;
        eq.Modelo = dto.Modelo;
        eq.NumeroSerie = dto.NumeroSerie;
        eq.DescricaoProblema = dto.DescricaoProblema;

        await _repo.UpdateAsync(id, eq);
        return await MapToDtoAsync(eq);
    }

    public async Task DeleteAsync(string id)
    {
        var eq = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Equipamento {id} não encontrado.");
        eq.Ativo = false;
        await _repo.UpdateAsync(id, eq);
    }

    private async Task<EquipamentoResponseDto> MapToDtoAsync(Equipamento eq)
    {
        var cliente = await _usuarioRepo.GetByIdAsync(eq.ClienteId);
        return new EquipamentoResponseDto(
            eq.Id, eq.ClienteId,
            cliente?.Nome ?? "Desconhecido",
            eq.Tipo, eq.Marca, eq.Modelo, eq.NumeroSerie,
            eq.DescricaoProblema, eq.CriadoEm);
    }
}
