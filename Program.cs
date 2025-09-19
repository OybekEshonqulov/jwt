using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Any;

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
              
            // Swagger config - YANGILANDI ✅
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { 
                    Title = "JWT Docker API", 
                    Version = "v1",
                    Description = "JWT Authentication API with logging features. " +
                                 "Endpoints: /api/logs/list, /api/logs/view/{filename}, /api/logs/delete/{filename}"
                });

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

                // ✅ LogData modelini Swagger ga qo'shish
                c.MapType<LogData>(() => new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["message"] = new OpenApiSchema 
                        { 
                            Type = "string", 
                            Description = "Log xabari (majburiy)" 
                        },
                        ["level"] = new OpenApiSchema 
                        { 
                            Type = "string", 
                            Description = "Log darajasi (ixtiyoriy - DEBUG, INFO, WARNING, ERROR, CRITICAL)",
                            Default = new OpenApiString("INFO")
                        }
                    },
                    Required = new HashSet<string> { "message" }
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

            // Log endpointini qo'shamiz - YANGILANDI ✅
            app.MapPost("/api/logs/write", 
                [Microsoft.AspNetCore.Mvc.ProducesResponseType(200)]
                [Microsoft.AspNetCore.Mvc.ProducesResponseType(400)]
                async (HttpContext context) => 
            {
                try
                {
                    // Content-Type ni tekshirish
                    if (!context.Request.HasJsonContentType())
                    {
                        return Results.BadRequest("Content-Type must be application/json");
                    }

                    var logData = await context.Request.ReadFromJsonAsync<LogData>();
                    if (logData == null) 
                        return Results.BadRequest("Invalid log data");
                    
                    // Message bo'sh bo'lmasligini tekshirish
                    if (string.IsNullOrEmpty(logData.Message))
                        return Results.BadRequest("Message maydoni majburiy");

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
        
                    return Results.Ok(new { 
                        success = true, 
                        file = fileName,
                        message = "Log muvaffaqiyatli yaratildi"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error: {ex.Message}");
                }
            }).Accepts<LogData>("application/json"); // ✅ Bu muhim!

            // Loglar ro'yxatini ko'rsatish
            app.MapGet("/api/logs/list", () =>
            {
                try
                {
                    var logDir = "/app/logs/";
                    
                    // Agar papka bo'lmasa
                    if (!Directory.Exists(logDir))
                        return Results.Ok(new { success = true, logs = new List<string>(), count = 0 });
                    
                    // Barcha log fayllarini olish
                    var logFiles = Directory.GetFiles(logDir, "*.txt")
                        .Select(filePath => new
                        {
                            name = Path.GetFileName(filePath),
                            size = new FileInfo(filePath).Length,
                            created = File.GetCreationTime(filePath),
                            modified = File.GetLastWriteTime(filePath),
                            sizeFormatted = FormatFileSize(new FileInfo(filePath).Length)
                        })
                        .OrderByDescending(f => f.modified) // Yangi loglar birinchi
                        .ToList();

                    return Results.Ok(new
                    {
                        success = true,
                        count = logFiles.Count,
                        logs = logFiles
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Xato: {ex.Message}");
                }
            });

            // Ma'lum bir log faylini o'qish
            app.MapGet("/api/logs/view/{filename}", (string filename) =>
            {
                try
                {
                    var logDir = "/app/logs/";
                    var filePath = Path.Combine(logDir, filename);
                    
                    // Xavfsizlik tekshiruvi - faqat .txt fayllar va log papkasida
                    if (!filePath.EndsWith(".txt") || !filePath.StartsWith(logDir))
                        return Results.BadRequest("Noto'g'ri fayl nomi");
                    
                    if (!File.Exists(filePath))
                        return Results.NotFound("Fayl topilmadi");
                    
                    // Fayl mazmunini o'qish
                    var content = File.ReadAllText(filePath);
                    var fileInfo = new FileInfo(filePath);
                    
                    return Results.Ok(new
                    {
                        success = true,
                        filename = filename,
                        content = content,
                        size = fileInfo.Length,
                        sizeFormatted = FormatFileSize(fileInfo.Length),
                        created = fileInfo.CreationTime,
                        modified = fileInfo.LastWriteTime
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Xato: {ex.Message}");
                }
            });

            // Log faylini o'chirish
            app.MapDelete("/api/logs/delete/{filename}", (string filename) =>
            {
                try
                {
                    var logDir = "/app/logs/";
                    var filePath = Path.Combine(logDir, filename);
                    
                    // Xavfsizlik tekshiruvi
                    if (!filePath.EndsWith(".txt") || !filePath.StartsWith(logDir))
                        return Results.BadRequest("Noto'g'ri fayl nomi");
                    
                    if (!File.Exists(filePath))
                        return Results.NotFound("Fayl topilmadi");
                    
                    File.Delete(filePath);
                    
                    return Results.Ok(new
                    {
                        success = true,
                        message = $"Fayl '{filename}' o'chirildi"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Xato: {ex.Message}");
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

            // System status endpointi
            app.MapGet("/api/system/status", () =>
            {
                return Results.Ok(new
                {
                    status = "Running",
                    environment = app.Environment.EnvironmentName,
                    time = DateTime.Now,
                    machine = Environment.MachineName,
                    os = Environment.OSVersion.Platform.ToString()
                });
            });

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "JWT Docker API v1");
                    c.RoutePrefix = "swagger"; // Swagger ni asosiy URL da ochish
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }

        // Fayl hajmini formatlash uchun yordamchi metod
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
    
    public class LogData
    {
        [Required(ErrorMessage = "Message maydoni majburiy")]
        public string Message { get; set; }
        
        public string Level { get; set; } = "INFO";
    }
}