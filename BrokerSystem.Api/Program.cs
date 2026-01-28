using BrokerSystem.Api.Common.Caching;
using BrokerSystem.Api.Common.Middleware;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// SWAGGER
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
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
                          .AllowAnyMethod());
});

var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<BrokerSystemDbContext>();
//    var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

//    try
//    {
//        var seeder = new DatabaseSeeder(context, logger);

//        await seeder.SeedAllAsync(resetDatabase: true);

//        logger.LogInformation("Seedowanie zakonczone!");
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Blad podczas seedowania!");
//    }
//}

// Configure the HTTP request pipeline.
app.MapControllers();

// SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS 
app.UseCors("AllowedBrokerSystemUI");

app.UseHttpsRedirection();
app.Run();
