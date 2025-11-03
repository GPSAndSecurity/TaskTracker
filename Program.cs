using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TaskTracker.Data;
using TaskTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// CONEXIÓN A MYSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not found in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Error de autenticación: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("Desafío de autenticación: " + context.Error);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validado con éxito.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

//  INYECCIÓN DE SERVICIOS
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ProyectoService>();
builder.Services.AddScoped<TareaService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<UbicacionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

//  CONFIG JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

//  CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", //website
                      "http://localhost:8100"
                      )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// PERMITIR ARCHIVOS MULTIPART HASTA 20MB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
});

var app = builder.Build();


//  MIDDLEWARE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

//  Servir archivos estáticos desde la carpeta "Uploads"
var UploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(UploadsPath),
    RequestPath = "/Uploads"
});


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
