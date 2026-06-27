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
        "Verifique appsettings.json e appsettings.Development.json.");
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

        // DIAGNÓSTICO BYTE-A-BYTE: o token TEM 2 pontos (confirmado), mas a
        // validação do .NET ainda rejeita como malformado. Isso normalmente
        // significa que existe algum caractere fora do alfabeto Base64URL
        // permitido (A-Z, a-z, 0-9, -, _) escondido em algum lugar do token
        // — um espaço, um "+", uma quebra de linha, etc. Esse bloco varre
        // caractere por caractere e aponta exatamente onde e qual é.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.Request.Path;
                var method = context.Request.Method;
                var authHeader = context.Request.Headers["Authorization"].ToString();

                Console.WriteLine($"[JWT] ── {method} {path}");

                if (string.IsNullOrEmpty(authHeader))
                {
                    Console.WriteLine($"[JWT]    sem header Authorization.");
                    return Task.CompletedTask;
                }

                var rawToken = authHeader.StartsWith("Bearer ")
                    ? authHeader["Bearer ".Length..].Trim()
                    : authHeader;

                var parts = rawToken.Split('.');
                Console.WriteLine($"[JWT]    {rawToken.Length} chars totais — Split('.') gerou {parts.Length} parte(s):");
                for (int i = 0; i < parts.Length; i++)
                    Console.WriteLine($"[JWT]      parte[{i}]: {parts[i].Length} chars");

                // Alfabeto Base64URL válido para cada segmento de um JWT
                var invalidos = new List<string>();
                for (int i = 0; i < rawToken.Length; i++)
                {
                    var c = rawToken[i];
                    bool valido = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                                  (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.';
                    if (!valido)
                        invalidos.Add($"posição {i}: '{c}' (U+{(int)c:X4})");
                }

                if (invalidos.Count > 0)
                {
                    Console.WriteLine($"[JWT]    ⚠⚠⚠ CARACTERES INVÁLIDOS ENCONTRADOS ({invalidos.Count}):");
                    foreach (var inv in invalidos.Take(10))
                        Console.WriteLine($"[JWT]        {inv}");
                }
                else
                {
                    Console.WriteLine($"[JWT]    ✓ todos os caracteres pertencem ao alfabeto Base64URL — nenhuma sujeira encontrada.");
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                Console.WriteLine($"[JWT] ✓✓✓ VALIDADO em {context.Request.Method} {context.Request.Path}");
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] ✗✗✗ FALHA em {context.Request.Method} {context.Request.Path}: " +
                    $"{context.Exception.GetType().Name} — {context.Exception.Message}");
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine($"[JWT] ⚠ CHALLENGE 401 em {context.Request.Method} {context.Request.Path}. " +
                    $"Erro: {context.Error}, Descrição: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Manutenção v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
