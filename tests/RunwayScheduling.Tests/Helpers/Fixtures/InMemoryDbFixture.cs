using Api.DataBase;
using Microsoft.EntityFrameworkCore;

namespace RunwayScheduling.Tests.Helpers.Fixtures;

public sealed class InMemoryDbFixture : IAsyncDisposable
{
    public AppDbContext DbContext { get; }

    public InMemoryDbFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        DbContext = new AppDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync() => await DbContext.DisposeAsync();
}
