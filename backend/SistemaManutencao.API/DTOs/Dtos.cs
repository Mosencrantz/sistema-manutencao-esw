namespace SistemaManutencao.API.DTOs;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public record LoginRequestDto(string Email, string Senha);

public record LoginResponseDto(
    string Token,
    string UserId,
    string Nome,
    string Email,
    string Perfil,
    DateTime Expiracao);

// ─── Usuario ──────────────────────────────────────────────────────────────────

public record UsuarioCreateDto(
    string Nome,
    string Email,
    string Senha,
    string Telefone,
    string Perfil,
    string? Endereco);  // só para Cliente

public record UsuarioUpdateDto(
    string Nome,
    string Telefone,
    string? Endereco);

public record AlterarSenhaDto(string SenhaAtual, string NovaSenha);

public record UsuarioResponseDto(
    string Id,
    string Nome,
    string Email,
    string Telefone,
    string Perfil,
    string? Endereco,
    bool Ativo,
    DateTime CriadoEm);

// ─── Equipamento ─────────────────────────────────────────────────────────────

public record EquipamentoCreateDto(
    string ClienteId,
    string Tipo,
    string Marca,
    string Modelo,
    string NumeroSerie,
    string DescricaoProblema);

public record EquipamentoUpdateDto(
    string Tipo,
    string Marca,
    string Modelo,
    string NumeroSerie,
    string DescricaoProblema);

public record EquipamentoResponseDto(
    string Id,
    string ClienteId,
    string NomeCliente,
    string Tipo,
    string Marca,
    string Modelo,
    string NumeroSerie,
    string DescricaoProblema,
    DateTime CriadoEm);

// ─── OrdemServico ─────────────────────────────────────────────────────────────

public record OrdemServicoCreateDto(
    string ClienteId,
    string EquipamentoId,
    string DescricaoProblema,
    string TipoEntrega,
    DateTime? PrevisaoConclusao,
    string? TecnicoId);

public record OrdemServicoUpdateStatusDto(
    string NovoStatus,
    string? Observacao);

public record PecaUtilizadaDto(
    string Nome,
    int Quantidade,
    decimal ValorUnitario);

public record OrdemServicoResponseDto(
    string Id,
    string Numero,
    string ClienteId,
    string NomeCliente,
    string EquipamentoId,
    string DescricaoEquipamento,
    string? TecnicoId,
    string? NomeTecnico,
    string DescricaoProblema,
    string Status,
    decimal ValorTotal,
    DateTime? PrevisaoConclusao,
    DateTime DataAbertura,
    DateTime? DataFinalizacao,
    string TipoEntrega,
    string Observacoes,
    List<PecaUtilizadaDto> PecasUtilizadas);

// ─── Diagnostico ─────────────────────────────────────────────────────────────

public record DiagnosticoCreateDto(
    string OrdemServicoId,
    string Descricao,
    string Observacoes);

public record DiagnosticoResponseDto(
    string Id,
    string OrdemServicoId,
    string TecnicoId,
    string NomeTecnico,
    string Descricao,
    string Observacoes,
    DateTime DataRegistro);

// ─── Arquivo ──────────────────────────────────────────────────────────────────

public record ArquivoResponseDto(
    string Id,
    string OrdemServicoId,
    string NomeArquivo,
    string TipoArquivo,
    long TamanhoBytes,
    DateTime DataUpload);
