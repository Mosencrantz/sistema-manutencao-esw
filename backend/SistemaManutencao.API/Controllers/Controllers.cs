// MVP: [Authorize] removido — sem autenticação
// CORREÇÃO: "admin" substituído por ObjectId válido (24 hex chars)
using Microsoft.AspNetCore.Mvc;
using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Services;

namespace SistemaManutencao.API.Controllers;

// ID fixo para o "admin" do MVP — precisa ser um ObjectId válido (24 chars hex)
file static class Mvp
{
    public const string AdminId = "aaaaaaaaaaaaaaaaaaaaaaaa";
}

// ─── Auth Controller ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => Ok(await _authService.LoginAsync(dto));

    [HttpGet("me")]
    public IActionResult Me()
        => Ok(new { id = Mvp.AdminId, perfil = "Administrador" });
}

// ─── Usuario Controller ───────────────────────────────────────────────────────

[ApiController]
[Route("api/usuarios")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;
    public UsuarioController(IUsuarioService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UsuarioUpdateDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/senha")]
    public async Task<IActionResult> AlterarSenha(string id, [FromBody] AlterarSenhaDto dto)
    {
        await _service.AlterarSenhaAsync(id, dto);
        return NoContent();
    }
}

// ─── Equipamento Controller ───────────────────────────────────────────────────

[ApiController]
[Route("api/equipamentos")]
public class EquipamentoController : ControllerBase
{
    private readonly IEquipamentoService _service;
    public EquipamentoController(IEquipamentoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("cliente/{clienteId}")]
    public async Task<IActionResult> GetByCliente(string clienteId)
        => Ok(await _service.GetByClienteIdAsync(clienteId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EquipamentoCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] EquipamentoUpdateDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

// ─── OrdemServico Controller ──────────────────────────────────────────────────

[ApiController]
[Route("api/ordens")]
public class OrdemServicoController : ControllerBase
{
    private readonly IOrdemServicoService _service;
    public OrdemServicoController(IOrdemServicoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("consulta/{numero}")]
    public async Task<IActionResult> GetByNumero(string numero)
        => Ok(await _service.GetByNumeroAsync(numero));

    [HttpGet("cliente/{clienteId}")]
    public async Task<IActionResult> GetByCliente(string clienteId)
        => Ok(await _service.GetByClienteIdAsync(clienteId));

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status)
        => Ok(await _service.GetByStatusAsync(status));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrdemServicoCreateDto dto)
    {
        // CORREÇÃO: usa ObjectId válido em vez de "admin"
        var created = await _service.CreateAsync(dto, Mvp.AdminId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> AtualizarStatus(
        string id, [FromBody] OrdemServicoUpdateStatusDto dto)
        => Ok(await _service.AtualizarStatusAsync(id, dto, Mvp.AdminId));

    [HttpPost("{id}/pecas")]
    public async Task<IActionResult> AdicionarPeca(
        string id, [FromBody] PecaUtilizadaDto peca)
        => Ok(await _service.AdicionarPecaAsync(id, peca, Mvp.AdminId));

    [HttpPatch("{id}/tecnico")]
    public async Task<IActionResult> AtribuirTecnico(
        string id, [FromBody] AtribuirTecnicoDto dto)
        => Ok(await _service.AtribuirTecnicoAsync(id, dto.TecnicoId, Mvp.AdminId));

    [HttpGet("{id}/historico")]
    public async Task<IActionResult> GetHistorico(string id)
        => Ok(await _service.GetHistoricoAsync(id));
}

public record AtribuirTecnicoDto(string TecnicoId);

// ─── Diagnostico Controller ───────────────────────────────────────────────────

[ApiController]
[Route("api/diagnosticos")]
public class DiagnosticoController : ControllerBase
{
    private readonly IDiagnosticoService _service;
    public DiagnosticoController(IDiagnosticoService service) => _service = service;

    [HttpGet("ordem/{osId}")]
    public async Task<IActionResult> GetByOrdem(string osId)
        => Ok(await _service.GetByOrdemServicoIdAsync(osId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiagnosticoCreateDto dto)
    {
        // CORREÇÃO: usa ObjectId válido em vez de "admin"
        var created = await _service.CreateAsync(dto, Mvp.AdminId);
        return Ok(created);
    }
}

// ─── Arquivo Controller ───────────────────────────────────────────────────────

[ApiController]
[Route("api/arquivos")]
public class ArquivoController : ControllerBase
{
    private readonly IArquivoService _service;
    public ArquivoController(IArquivoService service) => _service = service;

    [HttpGet("ordem/{osId}")]
    public async Task<IActionResult> GetByOrdem(string osId)
        => Ok(await _service.GetByOrdemServicoIdAsync(osId));

    [HttpPost("ordem/{osId}/upload")]
    public async Task<IActionResult> Upload(string osId, IFormFile file)
    {
        // CORREÇÃO: usa ObjectId válido em vez de "admin"
        var result = await _service.UploadAsync(osId, file, Mvp.AdminId);
        return Ok(result);
    }
}
