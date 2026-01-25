using BrokerSystem.Api.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// SWAGGER
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

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
        builder => builder.WithOrigins("http://127.0.0.1:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

var app = builder.Build();

// === SEEDING - URUCHAMIAJ TYLKO RAZ! ===
// Odkomentuj poni¿szy blok aby wykonaæ seedowanie

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<BrokerSystemDbContext>();
//    var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

//    try
//    {
//        var seeder = new DatabaseSeeder(context, logger);

//        //UWAGA: resetDatabase = true WYCZYŒCI CA£¥ BAZÊ!
//        await seeder.SeedAllAsync(resetDatabase: true);

//        logger.LogInformation("Seedowanie zakoñczone!");
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "B³¹d podczas seedowania!");
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
