using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SistemaManutencao.API.DTOs;
using SistemaManutencao.API.Models;
using SistemaManutencao.API.Repositories;
using SistemaManutencao.API.Settings;
using Microsoft.Extensions.Options;

namespace SistemaManutencao.API.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    string ObterPerfilDoToken(ClaimsPrincipal user);
    string ObterIdDoToken(ClaimsPrincipal user);
}

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUsuarioRepository usuarioRepo, IOptions<JwtSettings> jwtSettings)
    {
        _usuarioRepo = usuarioRepo;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var usuario = await _usuarioRepo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!usuario.Ativo)
            throw new UnauthorizedAccessException("Usuário desativado.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.Senha))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var expiracao = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);
        var token = GerarToken(usuario, expiracao);

        return new LoginResponseDto(token, usuario.Id, usuario.Nome,
            usuario.Email, usuario.Perfil, expiracao);
    }

    private string GerarToken(Usuario usuario, DateTime expiracao)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Perfil),
            new("perfil", usuario.Perfil)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiracao,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string ObterPerfilDoToken(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public string ObterIdDoToken(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
