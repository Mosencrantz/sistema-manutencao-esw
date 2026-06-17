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

    // Cria o administrador padrão se não houver nenhum usuário no banco
    public async Task SeedAdminAsync()
    {
        var todos = await _repo.GetAllAsync();
        if (todos.Count > 0) return;

        var admin = new Administrador
        {
            Nome = "Administrador",
            Email = "admin@sistema.com",
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
