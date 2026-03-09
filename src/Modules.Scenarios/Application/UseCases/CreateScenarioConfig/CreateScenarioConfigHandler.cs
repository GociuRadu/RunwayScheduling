using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateScenarioConfig;

public sealed class CreateScenarioConfigHandler
    : IRequestHandler<CreateScenarioConfigCommand, ScenarioConfig>
{
    private readonly IScenarioConfigStore _store;

    public CreateScenarioConfigHandler(IScenarioConfigStore store) => _store = store;

    public async Task<ScenarioConfig> Handle(CreateScenarioConfigCommand request, CancellationToken ct)
    {
        var cfg = new ScenarioConfig
        {
            AirportId = request.AirportId,
            Name = request.Name
        };

        if (request.StartTime is DateTime st) cfg.StartTime = st;
        if (request.EndTime is DateTime et) cfg.EndTime = et;

        if (request.Seed is int seed && seed != 0) cfg.Seed = seed;

        if (request.AircraftCount is int ac && ac > 0) cfg.AircraftCount = ac;
        if (request.AircraftDifficulty is int ad && ad > 0) cfg.AircraftDifficulty = ad;

        if (request.OnGroundAircraftCount is int og && og > 0) cfg.OnGroundAircraftCount = og;
        if (request.InboundAircraftCount is int ib && ib > 0) cfg.InboundAircraftCount = ib;
        if (request.RemainingOnGroundAircraftCount is int rg && rg > 0) cfg.RemainingOnGroundAircraftCount = rg;

        if (request.BaseSeparationSeconds is int bs && bs > 0) cfg.BaseSeparationSeconds = bs;

        if (request.WakePercent is int wp && wp > 0) cfg.WakePercent = wp;
        if (request.WeatherPercent is int wep && wep > 0) cfg.WeatherPercent = wep;

        if (request.WeatherIntervalCount is int wic && wic > 0) cfg.WeatherIntervalCount = wic;
        if (request.MinWeatherIntervalMinutes is int mw && mw > 0) cfg.MinWeatherIntervalMinutes = mw;
        if (request.WeatherDifficulty is int wd && wd > 0) cfg.WeatherDifficulty = wd;

        return await _store.Add(cfg, ct);
    }

}
