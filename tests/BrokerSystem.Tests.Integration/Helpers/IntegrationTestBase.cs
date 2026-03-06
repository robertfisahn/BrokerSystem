using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using BrokerSystem.Api.Infrastructure.Persistence.Context;
using System.Data.Common;

namespace BrokerSystem.Tests.Integration.Helpers;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;
    private readonly DbConnection _connection;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("IntegrationTest");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<BrokerSystemDbContext>));
                services.RemoveAll(typeof(BrokerSystemDbContext));

                services.AddDbContext<BrokerSystemDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        _client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BrokerSystemDbContext>();
        db.Database.EnsureCreated();
        TestDataSeeder.SeedAsync(db).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
