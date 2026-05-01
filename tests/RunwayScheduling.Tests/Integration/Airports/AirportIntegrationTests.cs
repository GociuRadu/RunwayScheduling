using Api.DataBase;
using Modules.Airports.Domain;
using RunwayScheduling.Tests.Helpers.Builders;
using RunwayScheduling.Tests.Helpers.Fixtures;

namespace RunwayScheduling.Tests.Integration.Airports;

public sealed class AirportIntegrationTests : IClassFixture<InMemoryDbFixture>
{
    private readonly AppDbContext _db;
    private readonly EfAirportStore _store;

    public AirportIntegrationTests(InMemoryDbFixture fixture)
    {
        _db = fixture.DbContext;
        _store = new EfAirportStore(_db);
    }

    [Fact]
    public async Task Add_PersistsToDatabase()
    {
        var airport = new AirportBuilder().WithName("LROP").Build();
        await _store.AddAsync(airport, CancellationToken.None);
        Assert.Contains(_db.Airports, a => a.Name == "LROP");
    }

    [Fact]
    public async Task GetAll_ReturnsAllPersistedAirports()
    {
        await _store.AddAsync(new AirportBuilder().WithName("EGLL").Build(), CancellationToken.None);
        await _store.AddAsync(new AirportBuilder().WithName("LFPG").Build(), CancellationToken.None);
        var airports = await _store.GetAllAsync(CancellationToken.None);
        Assert.Contains(airports, a => a.Name == "EGLL");
        Assert.Contains(airports, a => a.Name == "LFPG");
    }

    [Fact]
    public async Task Delete_RemovesAirportFromDatabase()
    {
        var airport = new AirportBuilder().WithName("KJFK").Build();
        await _store.AddAsync(airport, CancellationToken.None);
        var result = await _store.DeleteAsync(airport.Id, CancellationToken.None);
        Assert.True(result);
        Assert.DoesNotContain(_db.Airports, a => a.Id == airport.Id);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsFalse()
    {
        var result = await _store.DeleteAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.False(result);
    }
}
