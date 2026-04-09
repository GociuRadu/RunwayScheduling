using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;

public sealed class ScheduleDecoder : IScheduleDecoder
{
    public Chromosome BuildGreedyChromosome(ScenarioSnapshot snapshot)
    {
        var flights = snapshot.Flights;
        var order = Enumerable.Range(0, flights.Count)
            .OrderBy(i => flights[i].ScheduledTime)
            .ThenByDescending(i => flights[i].Priority)
            .ToArray();
        return new Chromosome(order);
    }

    public IReadOnlyList<SolvedFlight> Decode(Chromosome chromosome, ScenarioSnapshot snapshot)
    {
        var activeRunways = snapshot.Runways.Where(r => r.IsActive).ToList();
        var runwayAvailability = activeRunways.ToDictionary(r => r.Name, _ => snapshot.ScenarioConfig.StartTime);
        var result = new List<SolvedFlight>(chromosome.FlightOrder.Length);

        for (var i = 0; i < chromosome.FlightOrder.Length; i++)
        {
            var flight = snapshot.Flights[chromosome.FlightOrder[i]];

            var required = flight.Type == FlightType.Arrival ? RunwayType.Landing : RunwayType.Takeoff;
            var compatible = activeRunways
                .Where(r => r.RunwayType == RunwayType.Both || r.RunwayType == required)
                .ToList();

            if (compatible.Count == 0)
            {
                result.Add(Canceled(flight, i, CancellationReason.NoCompatibleRunway));
                continue;
            }

            Runway runway;
            DateTime assignedTime;
            WeatherInterval? weather;
            RandomEvent? randomEvent;

            while (true)
            {
                var best = compatible
                    .Select(r => (Runway: r, FreeAt: runwayAvailability[r.Name]))
                    .MinBy(c => c.FreeAt);

                runway = best.Runway;
                var freeAt = best.FreeAt;

                if (freeAt >= flight.ScheduledTime)
                    assignedTime = freeAt;
                else
                    assignedTime = flight.ScheduledTime;

                randomEvent = snapshot.RandomEvents.FirstOrDefault(e => assignedTime >= e.StartTime && assignedTime < e.EndTime);

                if (randomEvent is not null && randomEvent.ImpactPercent >= 100)
                {
                    runwayAvailability[runway.Name] = randomEvent.EndTime;
                    compatible = [.. compatible.Where(r => r.Name != runway.Name || runwayAvailability[r.Name] <= snapshot.ScenarioConfig.EndTime)];
                    if (compatible.Count == 0)
                        break;
                    continue;
                }

                break;
            }

            if (compatible.Count == 0)
            {
                result.Add(Canceled(flight, i, CancellationReason.NoCompatibleRunway));
                continue;
            }

            if (assignedTime < snapshot.ScenarioConfig.StartTime || assignedTime > snapshot.ScenarioConfig.EndTime)
            {
                result.Add(Canceled(flight, i, CancellationReason.OutsideScenarioWindow));
                continue;
            }

            var delay = (int)Math.Max(0, (assignedTime - flight.ScheduledTime).TotalMinutes);
            if (delay > flight.MaxDelayMinutes)
            {
                result.Add(Canceled(flight, i, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            weather = snapshot.WeatherIntervals
                .Where(w => w.StartTime <= assignedTime)
                .MaxBy(w => w.StartTime);

            var separation = CalculateSeparation(snapshot.ScenarioConfig, weather, randomEvent);

            result.Add(new SolvedFlight
            {
                FlightId = flight.Id,
                ScenarioConfigId = flight.ScenarioConfigId,
                AircraftId = flight.AircraftId,
                Callsign = flight.Callsign,
                Type = flight.Type,
                Priority = flight.Priority,
                ProcessingOrder = i,
                ScheduledTime = flight.ScheduledTime,
                MaxDelayMinutes = flight.MaxDelayMinutes,
                MaxEarlyMinutes = flight.MaxEarlyMinutes,
                Status = delay > 0 ? FlightStatus.Delayed : FlightStatus.Scheduled,
                CancellationReason = CancellationReason.None,
                AssignedRunway = runway.Name,
                AssignedTime = assignedTime,
                DelayMinutes = delay,
                EarlyMinutes = 0,
                SeparationAppliedSeconds = (int)separation.TotalSeconds,
                WeatherAtAssignment = weather?.WeatherType,
                AffectedByRandomEvent = randomEvent is not null
            });

            runwayAvailability[runway.Name] = assignedTime + separation;
        }

        return result;
    }

    private static TimeSpan CalculateSeparation(ScenarioConfig config, WeatherInterval? weather, RandomEvent? randomEvent)
    {
        var baseSeconds = config.BaseSeparationSeconds * (config.WakePercent / 100.0);

        var weatherMultiplier = weather is null
            ? config.WeatherPercent / 100.0
            : weather.WeatherType switch
            {
                WeatherCondition.Clear => 1.00,
                WeatherCondition.Cloud => 1.10,
                WeatherCondition.Rain  => 1.30,
                WeatherCondition.Snow  => 1.50,
                WeatherCondition.Fog   => 1.75,
                WeatherCondition.Storm => 2.00,
                _                      => 1.00
            };

        var eventMultiplier = randomEvent is not null
            ? 1.0 / (1.0 - randomEvent.ImpactPercent / 100.0)
            : 1.0;

        return TimeSpan.FromSeconds(baseSeconds * weatherMultiplier * eventMultiplier);
    }

    private static SolvedFlight Canceled(Flight flight, int order, CancellationReason reason) => new()
    {
        FlightId = flight.Id,
        ScenarioConfigId = flight.ScenarioConfigId,
        AircraftId = flight.AircraftId,
        Callsign = flight.Callsign,
        Type = flight.Type,
        Priority = flight.Priority,
        ProcessingOrder = order,
        ScheduledTime = flight.ScheduledTime,
        MaxDelayMinutes = flight.MaxDelayMinutes,
        MaxEarlyMinutes = flight.MaxEarlyMinutes,
        Status = FlightStatus.Canceled,
        CancellationReason = reason,
        AssignedRunway = null,
        AssignedTime = null,
        DelayMinutes = 0,
        EarlyMinutes = 0,
        SeparationAppliedSeconds = 0,
        WeatherAtAssignment = null,
        AffectedByRandomEvent = false
    };

    
}
