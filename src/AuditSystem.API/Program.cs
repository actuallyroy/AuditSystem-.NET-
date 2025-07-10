using AuditSystem.Domain.Repositories;
using AuditSystem.Domain.Services;
using AuditSystem.Domain.Entities;
using AuditSystem.Infrastructure.Data;
using AuditSystem.Infrastructure.Repositories;
using AuditSystem.Services;
using AuditSystem.API.Authorization;
using AuditSystem.API.SwaggerSchemaFilters;
using AuditSystem.API.Hubs;
using AuditSystem.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
builder.Configuration.AddEnvironmentVariables();

// Add logging with Serilog
Serilog.Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/audit_system_.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<AuditSystemDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure();
    });
    
    // Configure JSON handling for PostgreSQL
    options.LogTo(Console.WriteLine, LogLevel.Information);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// Add Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "AuditSystem";
});

// Add Redis connection multiplexer with error handling
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    var logger = provider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var redis = ConnectionMultiplexer.Connect(connectionString);
        logger.LogInformation("Redis connection established successfully");
        return redis;
    }
    catch (Exception ex)
    {
        logger.LogWarning("Failed to connect to Redis: {Message}. Application will continue without caching.", ex.Message);
        // Return a connection multiplexer that won't fail the application startup
        return ConnectionMultiplexer.Connect("audit_redis:6379,password=redis_password_123,abortConnect=false,connectTimeout=5000,syncTimeout=5000");
    }
});

// Add repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddScoped<IRepository<AuditSystem.Domain.Entities.OrganisationInvitation>, Repository<AuditSystem.Domain.Entities.OrganisationInvitation>>();

// Add notification repositories
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

// Add cache service with fallback
builder.Services.AddScoped<ICacheService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var redisConnection = provider.GetRequiredService<IConnectionMultiplexer>();
    
    try
    {
        // Test if Redis is actually working
        var database = redisConnection.GetDatabase();
        database.Ping();
        
        logger.LogInformation("Using Redis cache service");
        var distributedCache = provider.GetRequiredService<IDistributedCache>();
        var redisLogger = provider.GetRequiredService<ILogger<RedisCacheService>>();
        return new RedisCacheService(distributedCache, redisConnection, redisLogger);
    }
    catch (Exception ex)
    {
        logger.LogWarning("Redis not available, using null cache service: {Message}", ex.Message);
        var nullCacheLogger = provider.GetRequiredService<ILogger<NullCacheService>>();
        return new NullCacheService(nullCacheLogger);
    }
});

// Register original services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<OrganisationService>();

// Add HttpClient for external service calls
builder.Services.AddHttpClient();

// Register cached services as primary implementations
builder.Services.AddScoped<IUserService>(provider =>
{
    var originalService = provider.GetRequiredService<UserService>();
    var cacheService = provider.GetRequiredService<ICacheService>();
    var logger = provider.GetRequiredService<ILogger<CachedUserService>>();
    return new CachedUserService(originalService, cacheService, logger);
});

builder.Services.AddScoped<ITemplateService>(provider =>
{
    var originalService = provider.GetRequiredService<TemplateService>();
    var cacheService = provider.GetRequiredService<ICacheService>();
    var logger = provider.GetRequiredService<ILogger<CachedTemplateService>>();
    return new CachedTemplateService(originalService, cacheService, logger);
});

builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddScoped<IOrganisationService>(provider =>
{
    var originalService = provider.GetRequiredService<OrganisationService>();
    var cacheService = provider.GetRequiredService<ICacheService>();
    var logger = provider.GetRequiredService<ILogger<CachedOrganisationService>>();
    return new CachedOrganisationService(originalService, cacheService, logger);
});

builder.Services.AddScoped<DashboardCacheService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add notification service
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add RabbitMQ notification service and broadcast service
builder.Services.AddHostedService<RabbitMQNotificationService>();
builder.Services.AddHostedService<NotificationBroadcastService>();

// Add SignalR with improved configuration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Add controllers with improved JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Handle circular references
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Use camel case for properties
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add JWT authentication
var jwtIssuer = builder.Configuration["JWT:Issuer"];
var jwtAudience = builder.Configuration["JWT:Audience"];
var jwtSecret = builder.Configuration["JWT:Secret"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Add custom authorization
builder.Services.AddScoped<IAuthorizationHandler, CaseInsensitiveRoleHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrManager", policy =>
        policy.Requirements.Add(new CaseInsensitiveRoleRequirement("admin", "manager")));
    
    options.AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new CaseInsensitiveRoleRequirement("admin")));
    
    options.AddPolicy("ManagerOnly", policy =>
        policy.Requirements.Add(new CaseInsensitiveRoleRequirement("manager")));
    
    options.AddPolicy("AllRoles", policy =>
        policy.Requirements.Add(new CaseInsensitiveRoleRequirement("admin", "manager", "supervisor", "auditor")));
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Retail Execution Audit System API",
        Version = "v1",
        Description = "API for Retail Execution Audit System"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
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

    // Add custom schema filter for JsonElement types
    c.SchemaFilter<JsonElementSchemaFilter>();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
                "http://localhost:19006", 
                "http://localhost:3000", 
                "http://localhost:4200", 
                "http://localhost:8081",
                "https://test.scorptech.co"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.AutoReplenishment = true;
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueLimit = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Retail Execution Audit System API v1");
    });
    
    // In development, seed the database with test data if needed
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditSystemDbContext>();
        // You could add a seed method here if needed
        // await SeedData.Initialize(dbContext);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// Add a route group for API versioning
app.MapControllers().RequireAuthorization();

// Add SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

// Add health check endpoint
app.MapGet("/health", () => "Healthy").AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
