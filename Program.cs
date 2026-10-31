using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Infrastructure.Persistence;
using Siserp.Api.Infrastructure.Persistence.Repositories;
using Siserp.Api.Services;
using Siserp.Api.Services.Ple;

namespace Siserp.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Logging con log4net (archivo con rotación diaria + consola)
        builder.Logging.ClearProviders();
        builder.Logging.AddLog4Net("log4net.config");

        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Evita System.Text.Json.JsonException por ciclos en las relaciones de EF Core
                // (p. ej. Categoria->Productos->Categoria o Cliente->Comprobantes->Cliente).
                o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        builder.Services.AddDbContext<SiserpDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

        // JWT auth
        var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "change-this-secret-in-production";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                };
            });
        builder.Services.AddAuthorization();

        // CORS: permite que el frontend Angular (otro site de IIS) consuma la API
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SiserpFrontend", policy =>
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                              ?? ["http://localhost:4200", "https://enmikadosfact.geni-ia.com", "http://192.168.1.106:8090"];
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()
                    // Expone Content-Disposition para que el frontend pueda leer el nombre de
                    // archivo al descargar reportes/archivos (p. ej. los TXT del PLE SUNAT).
                    .WithExposedHeaders("Content-Disposition");
            });
        });

        // Swagger (disponible siempre, no solo en Development)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "SISERP API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header usando el esquema Bearer. Ej: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });

        // Infraestructura de persistencia hexagonal: adaptadores de escritura basados en
        // stored procedures (registrar/modificar/eliminación lógica) para las entidades maestras.
        builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        builder.Services.AddScoped<IClienteRepository, ClienteSqlRepository>();
        builder.Services.AddScoped<IProveedorRepository, ProveedorSqlRepository>();
        builder.Services.AddScoped<ICategoriaRepository, CategoriaSqlRepository>();
        builder.Services.AddScoped<ICentroCostoRepository, CentroCostoSqlRepository>();
        builder.Services.AddScoped<IProductoRepository, ProductoSqlRepository>();
        builder.Services.AddScoped<IEmpleadoRepository, EmpleadoSqlRepository>();
        // Compras / Órdenes de compra: a diferencia de los repositorios anteriores, reutilizan
        // la conexión/transacción del SiserpDbContext (ver ComprasSqlRepository /
        // OrdenCompraSqlRepository) en vez de abrir una conexión propia.
        builder.Services.AddScoped<IComprasRepository, ComprasSqlRepository>();
        builder.Services.AddScoped<IOrdenCompraRepository, OrdenCompraSqlRepository>();

        // Servicios de aplicacion / SUNAT
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<RolSeederService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<ProductosService>();
        builder.Services.AddScoped<ComprasService>();
        builder.Services.AddScoped<AutorizacionAdminService>();
        builder.Services.AddScoped<OrdenSalidaAlmacenService>();
        builder.Services.AddScoped<WebPushNotificationService>();
        builder.Services.AddScoped<UblBuilderService>();
        builder.Services.AddScoped<SunatSignerService>();
        builder.Services.AddScoped<SunatClientService>();
        builder.Services.AddSingleton<SunatQueueProcessor>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<SunatQueueProcessor>());

        // Marketing (campañas WhatsApp / correo)
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<WhatsAppService>();
        builder.Services.AddScoped<PdfService>();
        builder.Services.AddScoped<ContabilidadService>();
        builder.Services.AddScoped<EjercicioContableService>();
        builder.Services.AddScoped<ActivosFijosService>();
        builder.Services.AddScoped<ConciliacionBancariaService>();
        builder.Services.AddScoped<PleExportService>();
        builder.Services.AddScoped<ReporteFinancieroService>();

        var app = builder.Build();

        // Swagger UI habilitado en todos los entornos
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SISERP API v1");
        });

        app.UseCors("SiserpFrontend");

        // Sirve archivos estáticos subidos (p. ej. fotos de productos) desde wwwroot/uploads
        app.UseStaticFiles();


        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
