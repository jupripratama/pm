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

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
}

// ===== EPPlus License =====
ExcelPackage.License.SetNonCommercialOrganization("MKN");

// ✅ FORCE application timezone to UTC
TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "UTC");

// ===== Add Controllers =====
builder.Services.AddControllers(options =>
{
    // ✅ Register ResponseWrapperFilter globally
    options.Filters.Add<ResponseWrapperFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    
    // ✅ OPTIONAL: Ignore null values for cleaner response
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PM MKN API",
        Version = "v1",
        Description = "API PM & Documentation"
    });

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
    // Priority: Environment Variable > appsettings.json
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                        ?? builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("Connection string tidak ditemukan.");

    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
            mySqlOptions.CommandTimeout(180);
        }
    );

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.WebHost.UseUrls("http://*:5116");

// ===== JWT Authentication =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey tidak ditemukan.");


var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
            ?? jwtSettings["Issuer"];
            
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
            ?? jwtSettings["Audience"];

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // false karena development bisa http
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ===== Authorization =====
builder.Services.AddAuthorization(options =>
{
    options.AddCustomAuthorizationPolicies();
});

// ===== Services =====
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<ICallRecordService, CallRecordService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IInspeksiTemuanKpcService, InspeksiTemuanKpcService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// ===== Validators =====
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();

// ===== Cloudinary =====
builder.Services.Configure<CloudinarySettings>(options =>
{
    options.CloudName = builder.Configuration["Cloudinary:CloudName"] ?? "dz3rhkitn";
    options.ApiKey = builder.Configuration["Cloudinary:ApiKey"] ?? "565287517278285";
    options.ApiSecret = builder.Configuration["Cloudinary:ApiSecret"] ?? "VB7L7av5BE-Fi6bmyxWJziW2a5M";
});

builder.Services.AddHttpContextAccessor();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://pm.mknops.web.id",
            "http://localhost:3000",
            "http://localhost:5173",
            "https://pmfrontend-git-*.vercel.app",
            "https://*.vercel.app"
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
    options.MultipartBodyLengthLimit = 1073741824;
});

var app = builder.Build();

// ===== Middleware =====
app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PM MKN API V1 (DEV)"));
}
else
{
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PM MKN API V1"));
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===== SEEDING (Development Only) =====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Pastikan tabel sudah ada → JIKA BELUM, JALANKAN MIGRASI DULU!
        // Jika Anda belum buat migrasi, ganti dengan:
        // await context.Database.EnsureCreatedAsync();
        // TAPI LEBIH BAIK PAKAI MIGRASI

        await context.SeedInitialDataAsync(logger);
        logger.LogInformation("✅ Seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Seeding failed.");
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
app.Logger.LogInformation("DB Connection String: {Conn}", builder.Configuration.GetConnectionString("DefaultConnection"));

app.Run();