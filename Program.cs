using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Pm.Data;
using Pm.Services;
using Pm.Helper;
using Pm.Middleware;
using Pm.DTOs;
using Pm.Validators;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http.Features;
using Pm.DTOs.Auth;

var builder = WebApplication.CreateBuilder(args);

// ===== EPPlus License =====
ExcelPackage.License.SetNonCommercialOrganization("MKN");

// ===== Add Controllers =====
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ResponseWrapperFilter>(); // wrapper response global
});

// ===== Swagger Configuration =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PM MKN API",
        Version = "v1",
        Description = "API PM & Documentation"
    });

    // JWT di Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Gunakan format: Bearer {token}",
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ===== Database Context =====
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' tidak ditemukan. Pastikan sudah diatur di Render Environment Variables.");
    }
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// ===== JWT Authentication =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey tidak ditemukan di konfigurasi.");

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true; // aktifkan HTTPS di production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ===== Authorization Policy =====
builder.Services.AddAuthorization(options =>
{
    options.AddCustomAuthorizationPolicies();
});

// ===== Services =====
// User & Auth Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Role & Permission Services
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();

// Call Record Services
builder.Services.AddScoped<ICallRecordService, CallRecordService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();

// Cloudinary Service
builder.Services.Configure<CloudinarySettings>(options =>
{
    options.CloudName = builder.Configuration["Cloudinary:CloudName"] ?? "dz3rhkitn";
    options.ApiKey = builder.Configuration["Cloudinary:ApiKey"] ?? "565287517278285";
    options.ApiSecret = builder.Configuration["Cloudinary:ApiSecret"] ?? "VB7L7av5BE-Fi6bmyxWJziW2a5M";
});
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ===== Fluent Validation =====
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

// Inspeksi Temuan KPC Service
builder.Services.AddScoped<IInspeksiTemuanKpcService, InspeksiTemuanKpcService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpContextAccessor();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://pmfrontend.vercel.app",
            "http://localhost:3000",
            "http://localhost:5173",
            // TAMBAH INI untuk semua preview URLs
            "https://pmfrontend-*.vercel.app",
            "https://pmfrontend-git-*-jupripratamas-projects.vercel.app",
            "https://*.vercel.app" // ATAU INI untuk semua Vercel domains
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ===== Logging =====
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824; // 1GB
});


var app = builder.Build();

// ===== Middleware Pipeline =====
app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PM MKN API V1 (DEV)");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PM MKN API V1");
        c.RoutePrefix = string.Empty;
    });
}

// ===== Error Handling =====
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

// ===== Enable CORS untuk frontend =====
app.UseCors("AllowFrontend");

// ===== Authentication & Authorization =====
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== Database Migration (otomatis buat DB jika belum ada) =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure database is created
        context.Database.EnsureCreated();
        logger.LogInformation("✅ Database initialized successfully.");

        // Seed initial data (Roles, Permissions, Super Admin)
        await context.SeedInitialDataAsync(logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ An error occurred while initializing the database.");
        throw;
    }
}



app.Run();