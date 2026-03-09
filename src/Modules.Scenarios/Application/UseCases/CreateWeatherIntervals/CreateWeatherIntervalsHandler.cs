using MediatR;
using Modules.Scenarios.Domain;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;

namespace Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;

public sealed class CreateWeatherIntervalsHandler
    : IRequestHandler<CreateWeatherIntervalsCommand, IReadOnlyList<WeatherInterval>>
{
    private readonly IMediator _mediator;
    private readonly IWeatherIntervalStore _weatherStore;
    private readonly IScenarioConfigStore _configStore;

    public CreateWeatherIntervalsHandler(
        IMediator mediator,
        IWeatherIntervalStore weatherStore,
        IScenarioConfigStore configStore)
    {
        _mediator = mediator;
        _weatherStore = weatherStore;
        _configStore = configStore;
    }

    public async Task<IReadOnlyList<WeatherInterval>> Handle(
        CreateWeatherIntervalsCommand request,
        CancellationToken ct)
    {
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        if (cfg is null)
            throw new Exception("Scenario config not found");

        var existingDtos = await _mediator.Send(
            new WeatherIntervalsQuery(request.ScenarioConfigId),
            ct);

        if (existingDtos.Count > 0)
            throw new Exception("Weather intervals were already generated for this scenario");

        var response = GenerateWeatherIntervals(cfg);

        await _weatherStore.AddRange(response, ct);

        return response;
    }

    public List<WeatherInterval> GenerateWeatherIntervals(ScenarioConfig cfg)
    {
        var count = cfg.WeatherIntervalCount;
        var intervals = new List<WeatherInterval>(count);
        var totalMinutes = (int)(cfg.EndTime - cfg.StartTime).TotalMinutes;
        var random = new Random(cfg.Seed);
        var weatherDifficulty = cfg.WeatherDifficulty;
        var minWeatherIntervalMinutes = cfg.MinWeatherIntervalMinutes;
        var scenarioStartTime = cfg.StartTime;
        var scenarioEndTime = cfg.EndTime;

        if (count <= 0)
            return intervals;

        if (totalMinutes <= 0)
            throw new Exception("Scenario time window is invalid");

        if (minWeatherIntervalMinutes <= 0)
            throw new Exception("MinWeatherIntervalMinutes must be > 0");

        var minTotal = count * minWeatherIntervalMinutes;
        if (minTotal > totalMinutes)
            throw new Exception(
                $"Cannot fit {count} intervals of minimum {minWeatherIntervalMinutes} minutes into {totalMinutes} minutes");

        var durations = BuildDurations(count, minWeatherIntervalMinutes, totalMinutes, random);

        var cur = scenarioStartTime;
        for (int i = 0; i < count; i++)
        {
            var end = cur.AddMinutes(durations[i]);
            if (i == count - 1)
                end = scenarioEndTime;

            intervals.Add(new WeatherInterval
            {
                ScenarioConfigId = cfg.Id,
                StartTime = cur,
                EndTime = end,
                WeatherType = PickWeather(weatherDifficulty, random)
            });

            cur = end;
        }

        return intervals;
    }

    private static int[] BuildDurations(int count, int minMinutes, int totalMinutes, Random rnd)
    {
        var durations = new int[count];
        for (int i = 0; i < count; i++)
            durations[i] = minMinutes;

        var remaining = totalMinutes - (count * minMinutes);
        while (remaining > 0)
        {
            durations[rnd.Next(count)] += 1;
            remaining--;
        }

        return durations;
    }

    private static WeatherCondition PickWeather(int weatherDifficulty, Random rnd)
    {
        var d = weatherDifficulty;
        if (d < 1) d = 1;
        if (d > 5) d = 5;

        var wClear = 60 - 6 * (d - 1);
        var wCloud = 30 - 2 * (d - 1);
        var wRain = 12 + 4 * (d - 1);
        var wSnow = 4 + 2 * (d - 1);
        var wFog = 3 + 2 * (d - 1);
        var wStorm = 3 + 1 * (d - 1);

        var total = wClear + wCloud + wRain + wSnow + wFog + wStorm;
        var roll = rnd.Next(total);

        if ((roll -= wClear) < 0) return WeatherCondition.Clear;
        if ((roll -= wCloud) < 0) return WeatherCondition.Cloud;
        if ((roll -= wRain) < 0) return WeatherCondition.Rain;
        if ((roll -= wSnow) < 0) return WeatherCondition.Snow;
        if ((roll -= wFog) < 0) return WeatherCondition.Fog;
        return WeatherCondition.Storm;
    }
}