using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLNS.ERP.Data;
using QLNS_BE.Security;
using QLNS_BE.Services;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using Microsoft.AspNetCore.Http;
using QLNS_BE.Hubs;

namespace QLNS_BE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ================== 1. DB ========================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                       .UseSnakeCaseNamingConvention();
            });

            // ================== 2. JWT CONFIG =================
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
            if (jwt == null)
                throw new Exception("JWT config missing!");

            // ================== 3. DI =========================
            builder.Services.AddHttpContextAccessor(); // For generating full URLs in services

            // ✅ Memory cache cho face recognition optimization (6-8s → <3s)
            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuditLogService>();
            builder.Services.AddScoped<JwtTokenService>();
            builder.Services.AddScoped<PasswordHasher>();
            builder.Services.AddScoped<NhanVienService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<ChamCongService>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<DonPhepService>();
            builder.Services.AddScoped<DuAnService>();
            builder.Services.AddScoped<LuongService>();
            builder.Services.AddScoped<PhuCapService>();
            builder.Services.AddScoped<RoleService>();
            builder.Services.AddScoped<TaskService>();
            builder.Services.AddScoped<AccountWarningService>();
            builder.Services.AddScoped<PhongBanService>();
            builder.Services.AddScoped<YeuCauDieuChuyenService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<DeXuatGiamDocService>();
            builder.Services.AddScoped<ThongBaoService>();
            builder.Services.AddScoped<NoiLamViecService>();
            builder.Services.AddScoped<ThamSoHeThongService>();

            // ================== 3.1 FACE RECOGNITION ===========
            // Sử dụng InsightFace (ArcFace) với Python service
            var pythonApiUrl = builder.Configuration["FaceRecognition:PythonApiUrl"];
            var usePythonFace = !string.IsNullOrEmpty(pythonApiUrl);

            if (usePythonFace)
            {
                Console.WriteLine("✅ [FACE RECOGNITION] Sử dụng InsightFace (ArcFace) với Python service");
                Console.WriteLine($"📌 [FACE RECOGNITION] Python API: {pythonApiUrl}");
                Console.WriteLine($"📌 [FACE RECOGNITION] Threshold: {builder.Configuration["FaceRecognition:ConfidenceThreshold"]}");
                Console.WriteLine($"📌 [FACE RECOGNITION] Model: buffalo_l (512-dim embedding)\n");

                // HttpClient for Python API
                builder.Services.AddHttpClient("PythonFaceApi", client =>
                {
                    client.BaseAddress = new Uri(pythonApiUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                builder.Services.AddScoped<QLNS_BE.Services.FaceRecognition.IFaceRecognitionService,
                    QLNS_BE.Services.FaceRecognition.InsightFacePythonService>();
            }
            else
            {
                Console.WriteLine("⚠️  [FACE RECOGNITION] Sử dụng Simple Hash (fallback - không có Python API URL)");
                Console.WriteLine($"📌 [FACE RECOGNITION] Threshold: {builder.Configuration["FaceRecognition:ConfidenceThreshold"]}");
                Console.WriteLine("💡 [FACE RECOGNITION] Để dùng InsightFace: Thêm 'FaceRecognition:PythonApiUrl' vào appsettings\n");

                // Fallback: Simple hash (không cần Python service)
                builder.Services.AddScoped<QLNS_BE.Services.FaceRecognition.IFaceRecognitionService,
                    QLNS_BE.Services.FaceRecognition.SimpleFaceRecognitionService>();
            }

            builder.Services.AddScoped<FaceDataService>();

            // Gemini HttpClient (giữ lại cho các mục đích khác sau này)
            var geminiKey = builder.Configuration["Gemini:ApiKey"];
            if (!string.IsNullOrEmpty(geminiKey))
            {
                var timeoutSeconds = int.Parse(builder.Configuration["Gemini:TimeoutSeconds"] ?? "5");
                builder.Services.AddHttpClient("Gemini")
                    .ConfigureHttpClient(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    });
                Console.WriteLine("💡 [GEMINI] Gemini API Key đã cấu hình (sẵn sàng cho các tính năng khác)\n");
            }

            // ================== 3.5 SIGNALR ====================
            builder.Services.AddSignalR();

            // ================== 4. AUTH =======================
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = ClaimTypes.Role
                    };

                    // SignalR: Read JWT from query string for WebSocket connections
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // If the request is for SignalR hub
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // ================== 5. CONTROLLERS ================
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // ================== 6. SWAGGER ====================
            //builder.Services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo
            //    {
            //        Title = "QLNS ERP API",
            //        Version = "v1"
            //    });

            //    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //    {
            //        In = ParameterLocation.Header,
            //        Name = "Authorization",
            //        Description = "Nhập JWT token (không cần chữ Bearer)",
            //        Type = SecuritySchemeType.Http,
            //        Scheme = "bearer",
            //        BearerFormat = "JWT"
            //    });

            //    c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //    {
            //        {
            //            new OpenApiSecurityScheme
            //            {
            //                Reference = new OpenApiReference
            //                {
            //                    Type = ReferenceType.SecurityScheme,
            //                    Id = "Bearer"
            //                }
            //            },
            //            Array.Empty<string>()
            //        }
            //    });
            //});
            builder.Services.AddSwaggerGen(c =>
            {
                // ADD: tránh trùng tên DTO (schemaId) => fix swagger 500
                c.CustomSchemaIds(type => type.FullName);

                // ADD: swagger hiểu IFormFile
                c.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "QLNS ERP API",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Nhập JWT token (không cần chữ Bearer)",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
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


            //CORS - Allow all origins for development
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                {
                    policy
                        .SetIsOriginAllowed(_ => true)  // Allow any origin
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();  // Required for SignalR
                });
            });



            // ================== 7. PIPELINE ====================
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // CORS MUST be called BEFORE UseHttpsRedirection for preflight requests
            app.UseCors("AllowAngular");

            // app.UseHttpsRedirection(); // Commented out for local development
            // ================== STATIC FILES (uploads) ====================
            // ADD: đảm bảo có thư mục wwwroot/uploads/{avatars,duan,faces}
            var webRoot = app.Environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "avatars"));
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "duan"));
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "faces"));
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "faces", "thumbnails"));

            // ADD: map content-type cho doc/docx (để tải file đúng mime)
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".doc"] = "application/msword";
            provider.Mappings[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            provider.Mappings[".pdf"] = "application/pdf";

            // ADD: bật serve static files trong wwwroot (=> truy cập được /uploads/..)
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ================== SIGNALR HUB ====================
            app.MapHub<NotificationHub>("/hubs/notification");

            app.Run();
        }
    }
}
