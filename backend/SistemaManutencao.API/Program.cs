using System.Text;
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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(secretKey))
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema de Manutenção de Computadores",
        Version = "v1",
        Description = "API do sistema de gerenciamento de manutenção — UFS"
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
    //await usuarioService.SeedAdminAsync();
}

// ─── Middleware pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

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
