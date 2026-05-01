using Api.DataBase;
using Microsoft.EntityFrameworkCore;

namespace RunwayScheduling.Tests.Helpers.Fixtures;

public sealed class InMemoryDbFixture : IDisposable
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

    public void Dispose() => DbContext.Dispose();
}
