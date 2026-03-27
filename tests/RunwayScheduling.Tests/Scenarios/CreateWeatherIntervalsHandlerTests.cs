using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Domain;

namespace RunwayScheduling.Tests.Scenarios;

public sealed class CreateWeatherIntervalsHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IWeatherIntervalStore _weatherIntervalStore = Substitute.For<IWeatherIntervalStore>();
    private readonly IScenarioConfigStore _scenarioConfigStore = Substitute.For<IScenarioConfigStore>();

    [Fact]
    public void GenerateWeatherIntervals_CoversScenarioWindow()
    {
        var config = CreateScenarioConfig();
        var sut = new CreateWeatherIntervalsHandler(_mediator, _weatherIntervalStore, _scenarioConfigStore);

        var result = sut.GenerateWeatherIntervals(config);

        Assert.Equal(config.WeatherIntervalCount, result.Count);
        Assert.Equal(config.StartTime, result[0].StartTime);
        Assert.Equal(config.EndTime, result[^1].EndTime);
        Assert.All(result, interval => Assert.True(interval.StartTime < interval.EndTime));
    }

    [Fact]
    public async Task Handle_Throws_WhenIntervalsAlreadyExist()
    {
        var scenarioConfigId = Guid.NewGuid();
        var sut = new CreateWeatherIntervalsHandler(_mediator, _weatherIntervalStore, _scenarioConfigStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(
            TestEntityFactory.WithId(CreateScenarioConfig(), scenarioConfigId));
        _mediator.Send(Arg.Any<WeatherIntervalsQuery>(), Arg.Any<CancellationToken>()).Returns(
            [new WeatherIntervalsDto(Guid.NewGuid(), scenarioConfigId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10), WeatherCondition.Clear)]);

        var exception = await Assert.ThrowsAsync<Exception>(() => sut.Handle(new CreateWeatherIntervalsCommand(scenarioConfigId), CancellationToken.None));
        Assert.Equal("Weather intervals were already generated for this scenario", exception.Message);
    }

    [Fact]
    public void GenerateWeatherIntervals_Throws_WhenIntervalsCannotFit()
    {
        var config = CreateScenarioConfig();
        config.WeatherIntervalCount = 10;
        config.MinWeatherIntervalMinutes = 40;
        var sut = new CreateWeatherIntervalsHandler(_mediator, _weatherIntervalStore, _scenarioConfigStore);

        var exception = Assert.Throws<Exception>(() => sut.GenerateWeatherIntervals(config));
        Assert.Contains("Cannot fit", exception.Message);
    }

    private static ScenarioConfig CreateScenarioConfig() =>
        new()
        {
            StartTime = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            WeatherIntervalCount = 4,
            MinWeatherIntervalMinutes = 30,
            WeatherDifficulty = 3,
            Seed = 321
        };
}
