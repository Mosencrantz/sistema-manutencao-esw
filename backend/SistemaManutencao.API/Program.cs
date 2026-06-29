using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using SistemaManutencao.API.Middleware;
using SistemaManutencao.API.Repositories;
using SistemaManutencao.API.Services;
using SistemaManutencao.API.Settings;

var builder = WebApplication.CreateBuilder(args);

// ─── Porta dinâmica para plataformas de nuvem ────────────────────────────────
// Render, Railway e outras plataformas injetam a variável de ambiente PORT
// com a porta que o app DEVE escutar. Localmente essa variável não existe,
// então o comportamento de sempre (porta 5000, via launchSettings.json)
// continua funcionando sem nenhuma mudança.
var cloudPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(cloudPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{cloudPort}");
}

// ─── Settings ─────────────────────────────────────────────────────────────────
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");
var jwtSettings   = builder.Configuration.GetSection("JwtSettings");

builder.Services.Configure<MongoDbSettings>(mongoSettings);
builder.Services.Configure<JwtSettings>(jwtSettings);

// ─── MongoDB ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(mongoSettings["ConnectionString"]));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>()
      .GetDatabase(mongoSettings["DatabaseName"]));

// ─── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,     UsuarioRepository>();
builder.Services.AddScoped<IEquipamentoRepository, EquipamentoRepository>();
builder.Services.AddScoped<IOrdemServicoRepository,OrdemServicoRepository>();
builder.Services.AddScoped<IDiagnosticoRepository, DiagnosticoRepository>();
builder.Services.AddScoped<IArquivoRepository,     ArquivoRepository>();
builder.Services.AddScoped<IHistoricoOSRepository, HistoricoOSRepository>();

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,          AuthService>();
builder.Services.AddScoped<IUsuarioService,        UsuarioService>();
builder.Services.AddScoped<IEquipamentoService,    EquipamentoService>();
builder.Services.AddScoped<IOrdemServicoService,   OrdemServicoService>();
builder.Services.AddScoped<IDiagnosticoService,    DiagnosticoService>();
builder.Services.AddScoped<IArquivoService,        ArquivoService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var secretKey = jwtSettings["SecretKey"]!;

if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey está ausente ou tem menos de 32 caracteres. " +
        "Verifique appsettings.json e as variáveis de ambiente configuradas na plataforma de deploy.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew                = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────
// CORREÇÃO PARA DEPLOY: a lista de origens permitidas agora vem da configuração
// (appsettings.json ou variável de ambiente "AllowedOrigins__0", "AllowedOrigins__1"...)
// em vez de ficar fixa no código. Isso permite adicionar o domínio do frontend
// em produção (Vercel) só configurando a plataforma de deploy, sem precisar
// mudar e recompilar o código toda vez que o domínio mudar.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema de Manutenção de Computadores",
        Version = "v1",
        Description = "API do sistema de gerenciamento de manutenção de computadores"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insira o token JWT: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Seed initial admin ───────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    await usuarioService.SeedAdminAsync();
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// Swagger disponível também em produção, é útil pra testar a API já no ar.
// Se quiser desativar em produção depois, troque por: if (app.Environment.IsDevelopment())
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Manutenção v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
