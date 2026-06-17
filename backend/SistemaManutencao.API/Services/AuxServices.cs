using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Models;
using SistemaManutencao.API.Repositories;

namespace SistemaManutencao.API.Services;

// ─── Diagnostico ─────────────────────────────────────────────────────────────

public interface IDiagnosticoService
{
    Task<List<DiagnosticoResponseDto>> GetByOrdemServicoIdAsync(string osId);
    Task<DiagnosticoResponseDto> CreateAsync(DiagnosticoCreateDto dto, string tecnicoId);
}

public class DiagnosticoService : IDiagnosticoService
{
    private readonly IDiagnosticoRepository _repo;
    private readonly IUsuarioRepository _usuarioRepo;

    public DiagnosticoService(IDiagnosticoRepository repo, IUsuarioRepository usuarioRepo)
    {
        _repo = repo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<List<DiagnosticoResponseDto>> GetByOrdemServicoIdAsync(string osId)
    {
        var diagnosticos = await _repo.GetByOrdemServicoIdAsync(osId);
        var dtos = new List<DiagnosticoResponseDto>();
        foreach (var d in diagnosticos)
            dtos.Add(await MapToDtoAsync(d));
        return dtos;
    }

    public async Task<DiagnosticoResponseDto> CreateAsync(DiagnosticoCreateDto dto, string tecnicoId)
    {
        var diagnostico = new Diagnostico
        {
            OrdemServicoId = dto.OrdemServicoId,
            TecnicoId = tecnicoId,
            Descricao = dto.Descricao,
            Observacoes = dto.Observacoes
        };
        await _repo.CreateAsync(diagnostico);
        return await MapToDtoAsync(diagnostico);
    }

    private async Task<DiagnosticoResponseDto> MapToDtoAsync(Diagnostico d)
    {
        var tecnico = await _usuarioRepo.GetByIdAsync(d.TecnicoId);
        return new DiagnosticoResponseDto(
            d.Id, d.OrdemServicoId, d.TecnicoId,
            tecnico?.Nome ?? "Desconhecido",
            d.Descricao, d.Observacoes, d.DataRegistro);
    }
}

// ─── Arquivo ─────────────────────────────────────────────────────────────────

public interface IArquivoService
{
    Task<List<ArquivoResponseDto>> GetByOrdemServicoIdAsync(string osId);
    Task<ArquivoResponseDto> UploadAsync(string osId, IFormFile file, string usuarioId);
}

public class ArquivoService : IArquivoService
{
    private readonly IArquivoRepository _repo;
    private readonly IWebHostEnvironment _env;

    public ArquivoService(IArquivoRepository repo, IWebHostEnvironment env)
    {
        _repo = repo;
        _env = env;
    }

    public async Task<List<ArquivoResponseDto>> GetByOrdemServicoIdAsync(string osId)
    {
        var arquivos = await _repo.GetByOrdemServicoIdAsync(osId);
        return arquivos.Select(MapToDto).ToList();
    }

    public async Task<ArquivoResponseDto> UploadAsync(string osId, IFormFile file, string usuarioId)
    {
        // Valida tipo de arquivo
        var tiposPermitidos = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "video/avi", "application/pdf" };
        if (!tiposPermitidos.Contains(file.ContentType))
            throw new InvalidOperationException("Tipo de arquivo não permitido.");

        const long maxSize = 50 * 1024 * 1024; // 50 MB
        if (file.Length > maxSize)
            throw new InvalidOperationException("Arquivo excede o tamanho máximo de 50 MB.");

        // Cria diretório de upload
        var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", osId);
        Directory.CreateDirectory(uploadDir);

        var nomeUnico = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var caminho = Path.Combine(uploadDir, nomeUnico);

        await using var stream = File.Create(caminho);
        await file.CopyToAsync(stream);

        var arquivo = new Arquivo
        {
            OrdemServicoId = osId,
            NomeArquivo = file.FileName,
            TipoArquivo = file.ContentType,
            Caminho = Path.Combine("uploads", osId, nomeUnico),
            TamanhoBytes = file.Length,
            UploadadoPorId = usuarioId
        };

        await _repo.CreateAsync(arquivo);
        return MapToDto(arquivo);
    }

    private static ArquivoResponseDto MapToDto(Arquivo a) =>
        new(a.Id, a.OrdemServicoId, a.NomeArquivo, a.TipoArquivo, a.TamanhoBytes, a.DataUpload);
}
