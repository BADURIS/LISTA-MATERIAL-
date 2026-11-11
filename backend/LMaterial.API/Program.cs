using System;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using LMaterial.API.Middlewares;
using LMaterial.API.Seed;
using LMaterial.Application.Services.Auth;
using LMaterial.Application.Services.Locais;
using LMaterial.Application.Services.Materiais;
using LMaterial.Application.Services.Notificacoes;
using LMaterial.Application.Services.Planilhas;
using LMaterial.Application.Services.SemanasLista;
using LMaterial.Application.Validators.Materiais;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;
using LMaterial.Infrastructure.Context;
using LMaterial.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração de Controllers e OpenAPI/Swagger com suporte a JWT
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LMaterial.API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação via JWT. Digite 'Bearer ' seguido pelo token de acesso.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
builder.Services.AddOpenApi();

// 2. Configuração do DbContext com PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configuração de Autenticação JWT
// O segredo não tem valor padrão no código: deve vir de user-secrets, variável de ambiente ou cofre de segredos
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret não configurado (mínimo de 32 caracteres). Use dotnet user-secrets ou variável de ambiente.");

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "LMaterial.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "LMaterial.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Em produção, definir como true
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// 4. Autorização: todo endpoint exige autenticação; os perfis são aplicados por controller/ação com [Authorize(Roles = ...)]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 5. Configuração de CORS Restritivo
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "https://localhost:5001"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Configuração de Rate Limiting Nativo
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});

// 7. Registro de Injeção de Dependências - Infraestrutura
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMaterialRepository, MaterialRepository>();
builder.Services.AddScoped<ILocalRepository, LocalRepository>();
builder.Services.AddScoped<ISemanaListaRepository, SemanaListaRepository>();
builder.Services.AddScoped<IListaLocalRepository, ListaLocalRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();

// 8. Registro de Injeção de Dependências - Aplicação & Auth
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<ILocalService, LocalService>();
builder.Services.AddScoped<ISemanaListaService, SemanaListaService>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();
builder.Services.AddScoped<IPlanilhaSemanaService, PlanilhaSemanaService>();
builder.Services.AddScoped<LMaterial.Application.Services.Consumo.IConsumoService, LMaterial.Application.Services.Consumo.ConsumoService>();

// 9. Registro do FluentValidation (Assembly Scan)
builder.Services.AddValidatorsFromAssemblyContaining<CreateMaterialRequestValidator>();

var app = builder.Build();

// 10. Banco de dados e usuários iniciais
using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsDevelopment())
    {
        // Em desenvolvimento aplica as migrations pendentes automaticamente
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    await UsuarioSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration, app.Logger);
}

// 11. Pipeline de Middlewares HTTP
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// A interface fica no projeto Next.js (pasta frontend), que repassa /api para esta API
app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
