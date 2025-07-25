using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RemoteControl.ControlServer.Hubs;
using RemoteControl.ControlServer.Services;
using RemoteControl.Transport.Extensions;
using RemoteControl.Core.Data.Extensions;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/controlserver-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add database services
builder.Services.AddDatabase(builder.Configuration);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

// Add transport services
builder.Services.AddTransportServices(builder.Configuration);

// Add application services
builder.Services.AddSingleton<IAgentService, AgentService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<IViewerService, ViewerService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Transport:Jwt");
var signingKey = jwtSettings["SigningKey"];

if (string.IsNullOrEmpty(signingKey))
{
    throw new InvalidOperationException("JWT SigningKey is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Configure SignalR hub
app.MapHub<RemoteControlHub>("/hub");

// Configure API endpoints
app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => "Ready");
app.MapGet("/health/live", () => "Live");

// Minimal API endpoints for authentication
app.MapPost("/api/auth/token", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request.Username, request.Password);
    
    if (result.Success)
    {
        return Results.Ok(new { token = result.Token, userId = result.UserId });
    }
    
    return Results.Unauthorized();
});

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request.Username, request.Password, request.Email);
    
    if (result.Success)
    {
        return Results.Ok(new { message = "Registration successful", userId = result.UserId });
    }
    
    return Results.BadRequest(new { message = result.Message });
});

app.MapGet("/api/agents", async (IAgentService agentService) =>
{
    var agents = await agentService.GetOnlineAgentsAsync();
    return Results.Ok(agents);
}).RequireAuthorization();

// Migrate and seed database
await app.Services.MigrateDatabaseAsync();

Log.Information("Starting Remote Control Server on {Urls}", string.Join(", ", app.Urls));

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// DTOs
public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, string Email);

public record AuthResult(bool Success, string? Token = null, string? UserId = null, string? Message = null); 