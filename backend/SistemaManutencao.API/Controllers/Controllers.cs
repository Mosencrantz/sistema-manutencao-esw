// AUTENTICAÇÃO RESTAURADA: [Authorize] reativado em todos os controllers.
// O endpoint de consulta pública de OS (GET /ordens/consulta/{numero}) permanece
// [AllowAnonymous] propositalmente, pois o portal público (sem login) depende dele.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Services;

namespace SistemaManutencao.API.Controllers;

// ─── Auth Controller ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => Ok(await _authService.LoginAsync(dto));

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id     = _authService.ObterIdDoToken(User);
        var perfil = _authService.ObterPerfilDoToken(User);
        return Ok(new { id, perfil });
    }
}

// ─── Usuario Controller ───────────────────────────────────────────────────────

[ApiController]
[Route("api/usuarios")]
[Authorize] // qualquer usuário autenticado pode listar/consultar
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;
    public UsuarioController(IUsuarioService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    // Funcionário/Técnico precisam criar Clientes pela tela de Clientes —
    // por isso o perfil "Cliente" não tem permissão de criar/editar/remover usuários.
    [HttpPost]
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Update(string id, [FromBody] UsuarioUpdateDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Delete(string id)
    {
        // Defesa extra: o serviço também bloqueia remoção de Administradores
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
[Authorize]
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
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Create([FromBody] EquipamentoCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Update(string id, [FromBody] EquipamentoUpdateDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador,Funcionario,Tecnico")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

// ─── OrdemServico Controller ──────────────────────────────────────────────────

[ApiController]
[Route("api/ordens")]
[Authorize]
public class OrdemServicoController : ControllerBase
{
    private readonly IOrdemServicoService _service;
    private readonly IAuthService _auth;

    public OrdemServicoController(IOrdemServicoService service, IAuthService auth)
    {
        _service = service;
        _auth    = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    // Endpoint público — usado pelo portal de consulta sem necessidade de login
    [HttpGet("consulta/{numero}")]
    [AllowAnonymous]
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
        var userId = _auth.ObterIdDoToken(User);
        var created = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> AtualizarStatus(
        string id, [FromBody] OrdemServicoUpdateStatusDto dto)
    {
        var userId = _auth.ObterIdDoToken(User);
        return Ok(await _service.AtualizarStatusAsync(id, dto, userId));
    }

    [HttpPost("{id}/pecas")]
    public async Task<IActionResult> AdicionarPeca(
        string id, [FromBody] PecaUtilizadaDto peca)
    {
        var userId = _auth.ObterIdDoToken(User);
        return Ok(await _service.AdicionarPecaAsync(id, peca, userId));
    }

    [HttpPatch("{id}/tecnico")]
    public async Task<IActionResult> AtribuirTecnico(
        string id, [FromBody] AtribuirTecnicoDto dto)
    {
        var userId = _auth.ObterIdDoToken(User);
        return Ok(await _service.AtribuirTecnicoAsync(id, dto.TecnicoId, userId));
    }

    [HttpGet("{id}/historico")]
    public async Task<IActionResult> GetHistorico(string id)
        => Ok(await _service.GetHistoricoAsync(id));
}

public record AtribuirTecnicoDto(string TecnicoId);

// ─── Diagnostico Controller ───────────────────────────────────────────────────

[ApiController]
[Route("api/diagnosticos")]
[Authorize]
public class DiagnosticoController : ControllerBase
{
    private readonly IDiagnosticoService _service;
    private readonly IAuthService _auth;

    public DiagnosticoController(IDiagnosticoService service, IAuthService auth)
    {
        _service = service;
        _auth    = auth;
    }

    [HttpGet("ordem/{osId}")]
    public async Task<IActionResult> GetByOrdem(string osId)
        => Ok(await _service.GetByOrdemServicoIdAsync(osId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiagnosticoCreateDto dto)
    {
        var tecnicoId = _auth.ObterIdDoToken(User);
        var created   = await _service.CreateAsync(dto, tecnicoId);
        return Ok(created);
    }
}

// ─── Arquivo Controller ───────────────────────────────────────────────────────

[ApiController]
[Route("api/arquivos")]
[Authorize]
public class ArquivoController : ControllerBase
{
    private readonly IArquivoService _service;
    private readonly IAuthService _auth;

    public ArquivoController(IArquivoService service, IAuthService auth)
    {
        _service = service;
        _auth    = auth;
    }

    [HttpGet("ordem/{osId}")]
    public async Task<IActionResult> GetByOrdem(string osId)
        => Ok(await _service.GetByOrdemServicoIdAsync(osId));

    [HttpPost("ordem/{osId}/upload")]
    public async Task<IActionResult> Upload(string osId, IFormFile file)
    {
        var userId = _auth.ObterIdDoToken(User);
        var result = await _service.UploadAsync(osId, file, userId);
        return Ok(result);
    }
}
