using BrokerSystem.Api.Common.Auth;
using BrokerSystem.Api.Common.Caching;
using BrokerSystem.Api.Common.Middleware;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Dapper;
using BrokerSystem.Api.Common.Endpoints;
using System.Text;
using Microsoft.OpenApi.Models;

// Register Dapper Type Handlers (Must be early)
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

// QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// SWAGGER (API Documentation)
builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

// SIGNALR (Real-time communication)
builder.Services.AddSignalR();

// MEDIATR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// DATABASE CONTEXT
if (!builder.Environment.IsEnvironment("IntegrationTest"))
{
    builder.Services.AddDbContext<BrokerSystemDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedBrokerSystemUI",
        builder => builder.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// JWT AUTHENTICATION
var jwtSecret = builder.Configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Klucz JWT Secret nie został skonfigurowany.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CURRENT USER SERVICE
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// SEEDING
builder.Services.AddScoped<BrokerSystem.Api.Infrastructure.Persistence.Seeding.DatabaseSeeder>();

var app = builder.Build();

// INITIALIZE DATABASE (SEEDING)
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        var context = services.GetRequiredService<BrokerSystemDbContext>();
//        var seeder = services.GetRequiredService<BrokerSystem.Api.Infrastructure.Persistence.Seeding.DatabaseSeeder>();

//        // Ensure database is created (or migrate)
//        // Note: Using EnsureCreated() for simplicity in this project's current state
//        var resetDatabase = builder.Configuration.GetValue<bool>("Seed:Reset");

//        if (resetDatabase)
//        {
//            Console.WriteLine("Resetting database (EnsureDeleted)... This may take a minute.");
//            context.Database.EnsureDeleted();
//        }

//        if (context.Database.EnsureCreated())
//        {
//            Console.WriteLine("Database recreated successfully with latest schema.");
//        }

//        // Pass false to SeedAllAsync because we already cleared the DB via EnsureDeleted
//        await seeder.SeedAllAsync(resetDatabase: false);
//    }
//    catch (Exception ex)
//    {
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "An error occurred while seeding the database.");
//    }
//}

app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
// CORS 
app.UseCors("AllowedBrokerSystemUI");

app.UseHttpsRedirection();

// AUTH (order matters: Authentication before Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapAllEndpoints();

// SIGNALR HUB
app.MapHub<BrokerSystem.Api.Infrastructure.Hubs.BrokerHub>("/broker-hub");

// SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program
{
}
