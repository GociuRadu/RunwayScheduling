using Api.DataBase;
using RunwayScheduling.Tests.Helpers.Builders;
using RunwayScheduling.Tests.Helpers.Fixtures;

namespace RunwayScheduling.Tests.Integration.Scenarios;

public sealed class ScenarioConfigIntegrationTests : IClassFixture<InMemoryDbFixture>
{
    private readonly EfScenarioConfigStore _store;

    public ScenarioConfigIntegrationTests(InMemoryDbFixture fixture)
    {
        _store = new EfScenarioConfigStore(fixture.DbContext);
    }

    [Fact]
    public async Task Add_PersistsToDatabase()
    {
        var config = new ScenarioConfigBuilder().Build();

        var saved = await _store.Add(config, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("Test Scenario", saved.Name);
    }

    [Fact]
    public async Task GetById_ReturnsCorrectConfig()
    {
        var config = new ScenarioConfigBuilder().WithDifficulty(3).Build();
        await _store.Add(config, CancellationToken.None);

        var result = await _store.GetById(config.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(config.Id, result.Id);
        Assert.Equal(3, result.Difficulty);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        var result = await _store.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllConfigs()
    {
        await _store.Add(new ScenarioConfigBuilder().Build(), CancellationToken.None);
        await _store.Add(new ScenarioConfigBuilder().Build(), CancellationToken.None);

        var all = await _store.GetAll(CancellationToken.None);

        Assert.True(all.Count >= 2);
    }

    [Fact]
    public async Task Delete_RemovesFromDatabase()
    {
        var config = new ScenarioConfigBuilder().Build();
        await _store.Add(config, CancellationToken.None);

        var result = await _store.Delete(config.Id, CancellationToken.None);

        Assert.True(result);
        Assert.Null(await _store.GetById(config.Id, CancellationToken.None));
    }
}
