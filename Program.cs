using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace jwtDocker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);   

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5257); // Faqat HTTP port
            });

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
              
            // Swagger config
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // JWT authentication qo'shish
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT token kiriting: Bearer {your token}"
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
                        new string[] {}
                    }
                });
            });

            // JWT konfiguratsiyasi
            var key = Encoding.UTF8.GetBytes("super_secret_key_1234567890_super_long_key");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "yourapp",
                    ValidAudience = "yourapp",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Log endpointini qo'shamiz - DOCKER UCHUN MOSLASHTIRILGAN
            app.MapPost("/api/logs/write", async (HttpContext context) => 
            {
                try
                {
                    var logData = await context.Request.ReadFromJsonAsync<LogData>();
                    if (logData == null) return Results.BadRequest("Invalid log data");
        
                    // DOCKER UCHUN: /app/logs papkasiga yozamiz
                    var fileName = $"api_logs_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                    var directoryPath = "/app/logs/";
        
                    // Agar papka bo'lmasa yaratib olamiz
                    if (!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);
        
                    var fullPath = Path.Combine(directoryPath, fileName);
        
                    // Log ma'lumotlarini faylga yozamiz
                    var logContent = $"""
                    [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API LOG
                    Message: {logData.Message}
                    Level: {logData.Level}
                    IP: {context.Connection.RemoteIpAddress}
                    UserAgent: {context.Request.Headers.UserAgent}
                    ----------------------------------------
                    """;
        
                    await File.WriteAllTextAsync(fullPath, logContent);
        
                    return Results.Ok(new { success = true, file = fileName });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error: {ex.Message}");
                }
            });

            // Health check endpointi
            app.MapGet("/health", () => 
            {
                return Results.Ok(new { 
                    status = "Healthy", 
                    timestamp = DateTime.UtcNow,
                    version = "1.0" 
                });
            });
//top
            // System status endpointi
            app.MapGet("/api/system/status", () =>
            {
                return Results.Ok(new
                {
                    status = "Running",
                    environment = app.Environment.EnvironmentName,
                    time = DateTime.Now,
                    machine = Environment.MachineName
                });
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
    
    public class LogData
    {
        public string Message { get; set; }
        public string Level { get; set; } = "INFO";
    }
}