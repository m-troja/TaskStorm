using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        Path.Combine(_LogDir, _LogFilename + ".debug"),
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
        Path.Combine(_LogDir, _LogFilename + ".error"),
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
    DotNetEnv.Env.Load("dev.env");

    // -------------------
    // JWT Config
    // -------------------
    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT secret not configured.");
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? throw new InvalidOperationException("JWT issuer not configured.");
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
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

    });

    builder.Services.AddAuthorization();

    // -------------------
    // Replace default logging with Serilog
    // -------------------
    builder.Host.UseSerilog();

    // -------------------
    // Register services
    // -------------------
    builder.Services.AddDbContext<PostgresqlDbContext>(options =>
    {
        var dbName = Environment.GetEnvironmentVariable("TS_DB_NAME") ?? "TaskStorm";
        var dbHost = Environment.GetEnvironmentVariable("TS_DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("TS_DB_PORT") ?? "5432";
        var dbUser = Environment.GetEnvironmentVariable("TS_DB_USER") ?? "postgres";
        var dbPassword = Environment.GetEnvironmentVariable("TS_DB_PASSWORD") ?? "postgres";

        var connectionString =
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SearchPath=public";

        options.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention();
    });

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
    builder.Services.AddHttpClient<IChatGptService, ChatGptService>();
    builder.Services.AddHttpClient<ISlackNotificationService, SlackNotificationService>();
    builder.Services.AddScoped<UserCnv>();
    builder.Services.AddScoped<TeamCnv>();
    builder.Services.AddScoped<CommentCnv>();
    builder.Services.AddScoped<IssueCnv>();
    builder.Services.AddScoped<ProjectCnv>();
    builder.Services.AddScoped<RefreshTokenCnv>();
    builder.Services.AddScoped<ActivityCnv>();
    builder.Services.AddScoped<ISlackNotificationService, SlackNotificationService>();
    builder.Services.AddScoped<IFileService, FileService>();

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

    // set url before build()
    builder.WebHost.UseUrls($"http://0.0.0.0:{_HttpPort}");



    // File upload limits

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;  // 10MB
    });

    // Logging

    builder.Logging.AddDebug();
    builder.Logging.AddConsole();

    // Build the app

    var app = builder.Build();

    // Files

    var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");

    if (!Directory.Exists(uploadRoot))
        Directory.CreateDirectory(uploadRoot);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "uploads")),
        RequestPath = "/uploads"
    });


    // migrations
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
    Log.Fatal(ex, "Application terminated unexpectedly!");
    throw;
}


finally
{
    Log.CloseAndFlush();
}