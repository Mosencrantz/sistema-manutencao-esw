using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Models;
using SistemaManutencao.API.Repositories;

namespace SistemaManutencao.API.Services;

public interface IUsuarioService
{
    Task<List<UsuarioResponseDto>> GetAllAsync();
    Task<UsuarioResponseDto> GetByIdAsync(string id);
    Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto);
    Task<UsuarioResponseDto> UpdateAsync(string id, UsuarioUpdateDto dto);
    Task DeleteAsync(string id);
    Task AlterarSenhaAsync(string id, AlterarSenhaDto dto);
    Task SeedAdminAsync();
}

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;

    public UsuarioService(IUsuarioRepository repo) => _repo = repo;

    public async Task<List<UsuarioResponseDto>> GetAllAsync()
    {
        var usuarios = await _repo.GetAllAsync();
        return usuarios.Select(MapToDto).ToList();
    }

    public async Task<UsuarioResponseDto> GetByIdAsync(string id)
    {
        var u = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        return MapToDto(u);
    }

    public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto)
    {
        var existing = await _repo.GetByEmailAsync(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException("E-mail já cadastrado.");

        Usuario usuario = dto.Perfil switch
        {
            "Cliente"       => new Cliente { Endereco = dto.Endereco ?? string.Empty },
            "Funcionario"   => new Funcionario(),
            "Tecnico"       => new Tecnico(),
            "Administrador" => new Administrador(),
            _ => throw new ArgumentException($"Perfil inválido: {dto.Perfil}")
        };

        usuario.Nome = dto.Nome;
        usuario.Email = dto.Email;
        usuario.Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha);
        usuario.Telefone = dto.Telefone;

        await _repo.CreateAsync(usuario);
        return MapToDto(usuario);
    }

    public async Task<UsuarioResponseDto> UpdateAsync(string id, UsuarioUpdateDto dto)
    {
        var u = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");

        u.Nome = dto.Nome;
        u.Telefone = dto.Telefone;
        if (u is Cliente c && dto.Endereco is not null)
            c.Endereco = dto.Endereco;

        await _repo.UpdateAsync(id, u);
        return MapToDto(u);
    }

    public async Task DeleteAsync(string id)
    {
        var u = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");

        // Defesa em profundidade: administradores não podem ser removidos do sistema
        // (o frontend já bloqueia isso na UI, mas o backend também precisa garantir)
        if (u.Perfil == "Administrador")
            throw new InvalidOperationException("Administradores não podem ser removidos do sistema.");

        u.Ativo = false;
        await _repo.UpdateAsync(id, u);
    }

    public async Task AlterarSenhaAsync(string id, AlterarSenhaDto dto)
    {
        var u = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        u.AlterarSenha(dto.SenhaAtual, dto.NovaSenha);
        await _repo.UpdateAsync(id, u);
    }

    // Cria o administrador padrão se ele ainda não existir.
    // CORREÇÃO: antes verificava "se o banco tem qualquer usuário" — isso fazia
    // o seed nunca rodar em bancos compartilhados que já têm clientes/funcionários
    // cadastrados (como o banco usado pela equipe). Agora verifica especificamente
    // pelo e-mail do admin, garantindo que ele sempre exista, independente de quantos
    // outros usuários já estejam no banco.
    public async Task SeedAdminAsync()
    {
        const string adminEmail = "admin@sistema.com";

        var existente = await _repo.GetByEmailAsync(adminEmail);
        if (existente is not null) return; // admin já existe — não faz nada

        var admin = new Administrador
        {
            Nome = "Administrador",
            Email = adminEmail,
            Senha = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Telefone = ""
        };
        await _repo.CreateAsync(admin);
    }

    private static UsuarioResponseDto MapToDto(Usuario u) => new(
        u.Id, u.Nome, u.Email, u.Telefone, u.Perfil,
        u is Cliente c ? c.Endereco : null,
        u.Ativo, u.CriadoEm);
}
