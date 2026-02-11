using BrokerSystem.Api.Common.Caching;
using BrokerSystem.Api.Common.Middleware;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using BrokerSystem.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Dapper;

// Register Dapper Type Handlers (Must be early)
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

// QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// SWAGGER
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

// SIGNALR
builder.Services.AddSignalR();

// MEDIATR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
// DB CONTEXT
builder.Services.AddDbContext<BrokerSystemDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedBrokerSystemUI",
        builder => builder.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
});

var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
// CORS 
app.UseCors("AllowedBrokerSystemUI");

app.UseHttpsRedirection();

app.MapControllers();

// SIGNALR HUB
app.MapHub<BrokerSystem.Api.Infrastructure.Hubs.BrokerHub>("/broker-hub");

// SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
