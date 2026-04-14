using Modules.Scenarios.Domain;
using Modules.Solver.Application;
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
        var activeRunways = snapshot.Runways.Where(runway => runway.IsActive).ToList();
        var runwayAvailability = activeRunways.ToDictionary(runway => runway.Name, _ => snapshot.ScenarioConfig.StartTime);
        var result = new List<SolvedFlight>(chromosome.FlightOrder.Length);

        for (var i = 0; i < chromosome.FlightOrder.Length; i++)
        {
            var flight = snapshot.Flights[chromosome.FlightOrder[i]];
            var compatibleRunways = SchedulingRules.GetCompatibleRunways(activeRunways, flight.Type).ToList();

            if (compatibleRunways.Count == 0)
            {
                result.Add(SchedulingRules.CreateCanceledFlight(flight, i, CancellationReason.NoCompatibleRunway));
                continue;
            }

            DateTime assignedTime;
            RandomEvent? randomEvent;
            string assignedRunwayName;

            while (true)
            {
                var bestRunway = compatibleRunways
                    .Select(runway => (runway.Name, FreeAt: runwayAvailability[runway.Name]))
                    .MinBy(candidate => candidate.FreeAt);

                assignedRunwayName = bestRunway.Name;
                assignedTime = bestRunway.FreeAt >= flight.ScheduledTime
                    ? bestRunway.FreeAt
                    : flight.ScheduledTime;

                randomEvent = SchedulingRules.FindRandomEventAt(snapshot, assignedTime);
                if (randomEvent is null || randomEvent.ImpactPercent < 100)
                    break;

                runwayAvailability[assignedRunwayName] = randomEvent.EndTime;
                compatibleRunways = compatibleRunways
                    .Where(runway => runway.Name != assignedRunwayName || runwayAvailability[runway.Name] <= snapshot.ScenarioConfig.EndTime)
                    .ToList();

                if (compatibleRunways.Count == 0)
                    break;
            }

            if (compatibleRunways.Count == 0)
            {
                result.Add(SchedulingRules.CreateCanceledFlight(flight, i, CancellationReason.NoCompatibleRunway));
                continue;
            }

            if (assignedTime < snapshot.ScenarioConfig.StartTime || assignedTime > snapshot.ScenarioConfig.EndTime)
            {
                result.Add(SchedulingRules.CreateCanceledFlight(flight, i, CancellationReason.OutsideScenarioWindow));
                continue;
            }

            var delayMinutes = (int)Math.Max(0, (assignedTime - flight.ScheduledTime).TotalMinutes);
            if (delayMinutes > flight.MaxDelayMinutes)
            {
                result.Add(SchedulingRules.CreateCanceledFlight(flight, i, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            var weather = SchedulingRules.FindWeatherAt(snapshot, assignedTime);
            var separation = SchedulingRules.CalculateSeparation(snapshot.ScenarioConfig, weather, randomEvent?.ImpactPercent);

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
                Status = delayMinutes > 0 ? FlightStatus.Delayed : FlightStatus.Scheduled,
                CancellationReason = CancellationReason.None,
                AssignedRunway = assignedRunwayName,
                AssignedTime = assignedTime,
                DelayMinutes = delayMinutes,
                EarlyMinutes = 0,
                SeparationAppliedSeconds = (int)separation.TotalSeconds,
                WeatherAtAssignment = weather?.WeatherType,
                AffectedByRandomEvent = randomEvent is not null
            });

            runwayAvailability[assignedRunwayName] = assignedTime + separation;
        }

        return result;
    }
}
