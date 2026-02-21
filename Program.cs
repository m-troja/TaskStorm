using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TaskStorm.Data;
using TaskStorm.Exception.Handler;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;

// Load env variables
DotNetEnv.Env.Load("dev.env");
var _LogDir = Environment.GetEnvironmentVariable("TS_LOG_DIR") ?? "/home/michal";
var _LogFilename = Environment.GetEnvironmentVariable("TS_LOG_FILENAME") ?? "log";
var _HttpPort = Environment.GetEnvironmentVariable("TS_HTTP_PORT") ?? "6901";
var _LogPath = Path.Combine(_LogDir, _LogFilename);

// -------------------
// Configure Serilog
// -------------------
Log.Logger = new LoggerConfiguration()

    // Debug (including all below)

    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", Serilog.Events.LogEventLevel.Information)
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + "_.debug"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true
    )

    // Error
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + "_.error"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50_000_000,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true
    )

    //  Info (Info + Error) 
    .WriteTo.File(
        Path.Combine(_LogDir, _LogFilename + "_.info"),
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
    DotNetEnv.Env.Load("dev.env");

    // -------------------
    // JWT Config
    // -------------------
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer not configured.");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience not configured.");

    // Register JwtGenerator
    builder.Services.AddSingleton<JwtGenerator>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<JwtGenerator>>();
        return new JwtGenerator(config, logger);
    });

    // Authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    builder.Services.AddAuthorization();

    // -------------------
    // Replace default logging with Serilog
    // -------------------
    builder.Host.UseSerilog();

    // -------------------
    // Register services
    // -------------------
    builder.Services.AddDbContext<PostgresqlDbContext>();
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
    builder.Services.AddHttpClient<IChatGptService, ChatGptService>();
    builder.Services.AddScoped<UserCnv>();
    builder.Services.AddScoped<TeamCnv>();
    builder.Services.AddScoped<CommentCnv>();
    builder.Services.AddScoped<IssueCnv>();
    builder.Services.AddScoped<ProjectCnv>();
    builder.Services.AddScoped<PasswordService>();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    // -------------------
    // Swagger
    // -------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ALLOW CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // ustaw port PRZED build()
    builder.WebHost.UseUrls($"http://0.0.0.0:{_HttpPort}");

    // TEST Enchance debug for Dependency Injection issues
    builder.Logging.AddDebug();
    builder.Logging.AddConsole();

    var app = builder.Build();

    // migracje
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PostgresqlDbContext>();
        db.Database.Migrate();
    }

    app.UseMiddleware<GlobalExceptionHandler>();

    // TEST LOG REQUESTS

    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

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
    Log.Error(ex, "Application terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}