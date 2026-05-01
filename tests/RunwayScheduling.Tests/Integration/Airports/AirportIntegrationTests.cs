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
    public void Add_PersistsToDatabase()
    {
        var airport = new AirportBuilder().WithName("LROP").Build();

        _store.Add(airport);

        Assert.Contains(_db.Airports, a => a.Name == "LROP");
    }

    [Fact]
    public void GetAll_ReturnsAllPersistedAirports()
    {
        _store.Add(new AirportBuilder().WithName("EGLL").Build());
        _store.Add(new AirportBuilder().WithName("LFPG").Build());

        var airports = _store.GetAll();

        Assert.Contains(airports, a => a.Name == "EGLL");
        Assert.Contains(airports, a => a.Name == "LFPG");
    }

    [Fact]
    public async Task Delete_RemovesAirportFromDatabase()
    {
        var airport = new AirportBuilder().WithName("KJFK").Build();
        _store.Add(airport);

        var result = await _store.Delete(airport.Id, CancellationToken.None);

        Assert.True(result);
        Assert.DoesNotContain(_db.Airports, a => a.Id == airport.Id);
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsFalse()
    {
        var result = await _store.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }
}
