using System.Text;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using LabManagementAPI.Data;
using LabManagementAPI.Hubs;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Thiếu connection string. Hãy cấu hình ConnectionStrings__DefaultConnection.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

var dataProtectionKeysPath = builder.Configuration["Security:DataProtectionKeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("LabManagement");
var dataProtectionCertificatePath = builder.Configuration["Security:DataProtectionCertificatePath"];
if (!string.IsNullOrWhiteSpace(dataProtectionCertificatePath))
{
    if (!File.Exists(dataProtectionCertificatePath))
    {
        throw new InvalidOperationException("Không tìm thấy certificate mã hóa Data Protection.");
    }

    dataProtectionBuilder.ProtectKeysWithCertificate(
        X509CertificateLoader.LoadPkcs12FromFile(
            dataProtectionCertificatePath,
            builder.Configuration["Security:DataProtectionCertificatePassword"]));
}

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT key phải có ít nhất 32 ký tự. Hãy cấu hình Jwt__Key bằng secret của môi trường.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)
                    && context.HttpContext.Request.Path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var tokenVersionValue = context.Principal?.FindFirstValue("token_version");
                if (!int.TryParse(userIdValue, out var userId)
                    || !int.TryParse(tokenVersionValue, out var tokenVersion))
                {
                    context.Fail("Token không hợp lệ.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();
                var isValid = await dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(user => user.Id == userId
                        && user.IsActive
                        && user.Role == role
                        && user.TokenVersion == tokenVersion);
                if (!isValid)
                {
                    context.Fail("Tài khoản hoặc quyền truy cập đã thay đổi.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];

    options.AddPolicy("VueApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("sensitive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}:{httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
builder.Services.AddSignalR();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("mysql");

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
if (builder.Configuration.GetValue("ForwardedHeaders:TrustAll", false))
{
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
else
{
    foreach (var value in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
        ?? Array.Empty<string>())
    {
        if (IPAddress.TryParse(value, out var address))
        {
            forwardedHeadersOptions.KnownProxies.Add(address);
        }
    }
}
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

if (builder.Configuration.GetValue("Security:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}
app.UseCors("VueApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue("Database:ApplyMigrations", true))
    {
        await EnsureLegacyMigrationBaselineAsync(context, CancellationToken.None);
        await context.Database.MigrateAsync();
    }

    if (builder.Configuration.GetValue("Seed:Enabled", false))
    {
        await DbInitializer.SeedDevelopmentDataAsync(context, builder.Configuration);
    }
}

static async Task EnsureLegacyMigrationBaselineAsync(AppDbContext context, CancellationToken cancellationToken)
{
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync(cancellationToken);
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '__EFMigrationsHistory';";
    var hasHistoryTable = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    if (!hasHistoryTable)
    {
        return;
    }

    command.CommandText = "SELECT COUNT(*) FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260808063057_InitialCreate';";
    var hasCurrentBaseline = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    if (hasCurrentBaseline)
    {
        return;
    }

    // The repository originally had a legacy migration chain ending at this ID.
    // A previous branch replaced that chain with InitialCreate; mark only the
    // known legacy endpoint as its equivalent so existing databases can continue
    // with the new feature migrations without recreating tables.
    command.CommandText = "SELECT COUNT(*) FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260801090000_AddConsumableTransactions';";
    var isLegacyDatabase = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    if (isLegacyDatabase)
    {
        command.CommandText = "INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260808063057_InitialCreate', '9.0.0');";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

await app.RunAsync();

public partial class Program;
