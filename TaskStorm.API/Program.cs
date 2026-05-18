using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using TaskStorm.Data;
using TaskStorm.Exception.Handler;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Security;
using TaskStorm.Security.Impl;
using TaskStorm.Service;
using TaskStorm.Service.Impl;
using MassTransit;
using TaskStorm.Event.Consumers;
using TaskStorm.Event.Hubs;

if (File.Exists("dev.env"))
{
    DotNetEnv.Env.Load("dev.env");
}

var _LogDir = Environment.GetEnvironmentVariable("TS_LOG_DIR") ?? "/home/michal";
var _LogFilename = Environment.GetEnvironmentVariable("TS_LOG_FILENAME") ?? "log";
var _HttpPort = Environment.GetEnvironmentVariable("TS_HTTP_PORT") ?? "6901";
var _LogPath = Path.Combine(_LogDir, _LogFilename);

// -------------------
//  Serilog
// -------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", Serilog.Events.LogEventLevel.Information)
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + ".debug"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true
    )
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + ".error"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true
    )
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + ".info"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true
    )
    .CreateLogger();

try
{
    Log.Information("Starting TaskStorm WebApplication...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();

    // -------------------
    // JWT 
    // -------------------
    var jwtSecret = builder.Configuration["JWT_SECRET"] ?? builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT secret not configured.");
    var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("JWT issuer not configured.");
    var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("JWT audience not configured.");

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
    builder.Host.UseSerilog();

    // -------------------
    // Register Database
    // -------------------
    builder.Services.AddDbContext<PostgresqlDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            var dbName = builder.Configuration["TS_DB_NAME"] ?? "taskstorm";
            var dbHost = builder.Configuration["TS_DB_HOST"] ?? "localhost";
            var dbPort = builder.Configuration["TS_DB_PORT"] ?? "5432";
            var dbUser = builder.Configuration["TS_DB_USER"] ?? "task_user";
            var dbPassword = builder.Configuration["TS_DB_PASSWORD"] ?? "taskstorm";

            connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SearchPath=public";
        }

        Log.Information("Initializing Database Connection...");
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    });

    // -------------------
    // Dependency Injection
    // -------------------
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IActivityService, ActivityService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IIssueService, IssueService>();
    builder.Services.AddScoped<IRegisterService, RegisterService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<ILoginService, LoginService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ITeamService, TeamService>();
    builder.Services.AddScoped<IChatGptService, ChatGptService>();
    builder.Services.AddScoped<IPasswordService, PasswordService>();
    builder.Services.AddScoped<IJwtGenerator, JwtGenerator>();
    builder.Services.AddScoped<IMasterdataService, MasterdataService>();
    builder.Services.AddHttpClient<IChatGptService, ChatGptService>();
    builder.Services.AddHttpClient<ISlackNotificationService, SlackNotificationService>();
    builder.Services.AddScoped<TaskStorm.Service.INotificationService, TaskStorm.Service.Impl.NotificationService>();

    builder.Services.AddScoped<UserCnv>();
    builder.Services.AddScoped<TeamCnv>();
    builder.Services.AddScoped<CommentCnv>();
    builder.Services.AddScoped<IssueCnv>();
    builder.Services.AddScoped<ProjectCnv>();
    builder.Services.AddScoped<RefreshTokenCnv>();
    builder.Services.AddScoped<ActivityCnv>();
    builder.Services.AddScoped<IFileService, FileService>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // -------------------
    // CORS 
    // -------------------
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendCorsPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    builder.Services.AddSignalR();

    // -------------------
    // MassTransit & RabbitMQ 
    // -------------------
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<IssueEventsConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitHost = builder.Configuration["RABBITMQ_HOST"] ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var username = builder.Configuration["RABBITMQ_USERNAME"] ?? builder.Configuration["RabbitMQ:Username"] ?? "admin";
            var password = builder.Configuration["RABBITMQ_PASSWORD"] ?? builder.Configuration["RabbitMQ:Password"] ?? "admin";

            Log.Information("Connecting MassTransit to RabbitMQ Host: {Host}", rabbitHost);

            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(username);
                h.Password(password);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    builder.WebHost.UseUrls($"http://0.0.0.0:{_HttpPort}");

    // File upload size limits 
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    });

    builder.Logging.AddDebug();
    builder.Logging.AddConsole();

    var app = builder.Build();

    // Ensure static files directory structure exists
    var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
    if (!Directory.Exists(uploadRoot))
        Directory.CreateDirectory(uploadRoot);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = "/uploads"
    });

    // Automatically apply database schema migrations on container startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PostgresqlDbContext>();
        db.Database.Migrate();
    }

    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseRouting();

    // Enable unified CORS policy globally
    app.UseCors("FrontendCorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseStaticFiles();
    app.MapControllers();

    app.MapHub<NotificationHub>("/notificationHub");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    Log.Information($"TaskStorm WebApplication started successfully on port {_HttpPort}");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}