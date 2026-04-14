using MediatR;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Aircrafts.Domain;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateFlights;

public sealed class CreateFlightsHandler : IRequestHandler<CreateFlightsCommand, List<Flight>>
{
    private const int TurnaroundMinutes = 20;

    private readonly IMediator _mediator;
    private readonly IScenarioConfigStore _configStore;
    private readonly IFlightStore _flightStore;

    public CreateFlightsHandler(
        IMediator mediator,
        IScenarioConfigStore configStore,
        IFlightStore flightStore)
    {
        _mediator = mediator;
        _configStore = configStore;
        _flightStore = flightStore;
    }

    public async Task<List<Flight>> Handle(CreateFlightsCommand request, CancellationToken ct)
    {
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        if (cfg is null)
            throw new Exception("Scenario config not found");

        ValidateConfig(cfg);

        var aircrafts = await _mediator.Send(
            new GenerateRandomAircraftCommand(cfg.AircraftCount, cfg.AircraftDifficulty, cfg.Id),
            ct);

        if (aircrafts.Count < cfg.AircraftCount)
            throw new Exception("Generated aircraft count must be >= ScenarioConfig.AircraftCount");

        var flights = GenerateFlightsFromAircrafts(aircrafts, cfg);

        await _flightStore.AddRange(flights, ct);
        await _flightStore.SaveChanges(ct);

        return flights;
    }

    private static void ValidateConfig(ScenarioConfig cfg)
    {
        if (cfg.OnGroundAircraftCount + cfg.InboundAircraftCount != cfg.AircraftCount)
            throw new Exception("Invalid config: OnGroundAircraftCount + InboundAircraftCount must equal AircraftCount");

        if (cfg.RemainingOnGroundAircraftCount < 0 || cfg.RemainingOnGroundAircraftCount > cfg.AircraftCount)
            throw new Exception("Invalid config: RemainingOnGroundAircraftCount must be in [0..AircraftCount]");

        var duration = cfg.EndTime - cfg.StartTime;
        if (duration.TotalMinutes < 10)
            throw new Exception("Invalid config: Scenario interval must be at least 10 minutes (EndTime - StartTime >= 10).");
    }

    private static List<Flight> GenerateFlightsFromAircrafts(List<Aircraft> aircrafts, ScenarioConfig cfg)
    {
        var rng = new Random(cfg.Seed);
        var inboundPool = aircrafts
            .Skip(cfg.OnGroundAircraftCount)
            .Take(cfg.InboundAircraftCount)
            .ToList();
        var stayCount = cfg.RemainingOnGroundAircraftCount;
        var departCount = cfg.AircraftCount - stayCount;
        var (safeStart, safeEnd) = GetSafeWindow(cfg.StartTime, cfg.EndTime, preferredMarginMinutes: 10);
        var allAircraftIds = aircrafts.Select(a => a.Id).ToList();
        ShuffleInPlace(allAircraftIds, rng);
        var departingAircraftIds = allAircraftIds
            .Skip(stayCount)
            .Take(departCount)
            .ToList();
        var arrivalTimes = GenerateScheduleTimes(safeStart, safeEnd, inboundPool.Count, rng);
        var inboundArrivalByAircraft = new Dictionary<Guid, DateTime>(inboundPool.Count);
        var flights = new List<Flight>(inboundPool.Count + departCount);
        var callIndex = 1;

        for (int i = 0; i < inboundPool.Count; i++)
        {
            var ac = inboundPool[i];
            var (maxDelay, maxEarly) = GetTimingLimits(
                cfg.Difficulty,
                ac.WakeCategory,
                FlightType.Arrival,
                rng);

            var arrTime = RoundToNearestMinute(Clamp(arrivalTimes[i], safeStart, safeEnd));
            inboundArrivalByAircraft[ac.Id] = arrTime;

            flights.Add(new Flight
            {
                Callsign = $"FLT{callIndex++:000}",
                ScenarioConfigId = cfg.Id,
                AircraftId = ac.Id,
                Type = FlightType.Arrival,
                ScheduledTime = arrTime,
                MaxDelayMinutes = maxDelay,
                MaxEarlyMinutes = maxEarly,
                Priority = GeneratePriority(rng)
            });
        }

        var departureTimes = GenerateScheduleTimes(safeStart, safeEnd, departCount, rng);
        departureTimes.Sort();
        ShuffleInPlace(departingAircraftIds, rng);

        for (int i = 0; i < departingAircraftIds.Count; i++)
        {
            var aircraftId = departingAircraftIds[i];

            var ac = aircrafts.First(a => a.Id == aircraftId);
            var (maxDelay, maxEarly) = GetTimingLimits(
                cfg.Difficulty,
                ac.WakeCategory,
                FlightType.Departure,
                rng);

            DateTime depTime;

            if (inboundArrivalByAircraft.TryGetValue(aircraftId, out var arrTime))
            {
                var earliestDeparture = arrTime.AddMinutes(TurnaroundMinutes);

                if (earliestDeparture >= safeEnd)
                    continue;

                var availableWindowMinutes = (safeEnd - earliestDeparture).TotalMinutes;

                depTime = RoundToNearestMinute(
                    earliestDeparture.AddMinutes(rng.NextDouble() * availableWindowMinutes));
            }
            else
            {
                var availableWindowMinutes = (safeEnd - safeStart).TotalMinutes;

                depTime = RoundToNearestMinute(
                    safeStart.AddMinutes(rng.NextDouble() * availableWindowMinutes));
            }

            depTime = Clamp(depTime, safeStart, safeEnd);

            flights.Add(new Flight
            {
                Callsign = $"FLT{callIndex++:000}",
                ScenarioConfigId = cfg.Id,
                AircraftId = aircraftId,
                Type = FlightType.Departure,
                ScheduledTime = depTime,
                MaxDelayMinutes = maxDelay,
                MaxEarlyMinutes = maxEarly,
                Priority = GeneratePriority(rng)
            });
        }
        flights.Sort((a, b) => a.ScheduledTime.CompareTo(b.ScheduledTime));
        return flights;
    }

    private static (DateTime safeStart, DateTime safeEnd) GetSafeWindow(DateTime start, DateTime end, int preferredMarginMinutes)
    {
        if (end < start)
            end = start;

        var totalMinutes = (end - start).TotalMinutes;
        if (totalMinutes <= 0)
            return (start, start);

        var maxMargin = Math.Floor(totalMinutes / 2.0);
        var margin = Math.Min(preferredMarginMinutes, (int)maxMargin);

        var safeStart = start.AddMinutes(margin);
        var safeEnd = end.AddMinutes(-margin);

        if (safeEnd < safeStart)
            return (start, end);

        return (safeStart, safeEnd);
    }

    private static DateTime Clamp(DateTime t, DateTime min, DateTime max)
    {
        if (t < min) return min;
        if (t > max) return max;
        return t;
    }

    private static void ShuffleInPlace<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static List<DateTime> GenerateScheduleTimes(
        DateTime start, DateTime end, int count, Random rng, int minGapMinutes = 0)
    {
        var times = new List<DateTime>(count);

        if (count <= 0)
            return times;

        if (end < start)
            end = start;

        var windowMinutes = (end - start).TotalMinutes;
        if (windowMinutes <= 0)
        {
            for (int i = 0; i < count; i++)
                times.Add(start);

            return times;
        }

        var minutes = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            minutes.Add(rng.NextDouble() * windowMinutes);
        }

        minutes.Sort();

        if (minGapMinutes > 0 && count > 1)
        {
            for (int i = 1; i < minutes.Count; i++)
            {
                var minAllowed = minutes[i - 1] + minGapMinutes;
                if (minutes[i] < minAllowed)
                    minutes[i] = minAllowed;
            }

            var last = minutes[^1];
            if (last > windowMinutes)
            {
                var shiftBack = last - windowMinutes;
                for (int i = 0; i < minutes.Count; i++)
                    minutes[i] -= shiftBack;

                for (int i = 0; i < minutes.Count; i++)
                    if (minutes[i] < 0) minutes[i] = 0;
            }
        }

        for (int i = 0; i < minutes.Count; i++)
        {
            var t = RoundToNearestMinute(start.AddMinutes(minutes[i]));
            if (t < start) t = start;
            if (t > end) t = end;
            times.Add(t);
        }

        return times;
    }

    private static (int maxDelay, int maxEarly) GetTimingLimits(
        int difficulty,
        WakeTurbulenceCategory wake,
        FlightType flightType,
        Random rng)
    {
        if (difficulty < 1) difficulty = 1;
        if (difficulty > 5) difficulty = 5;

        var difficultyRatio = (difficulty - 1) / 4.0;
        var baseDelay = (int)Math.Round(34 - 16 * difficultyRatio);
        var baseEarly = flightType == FlightType.Arrival
            ? (int)Math.Round(3 - 2 * difficultyRatio)
            : (int)Math.Round(7 - 3 * difficultyRatio);

        var (wakeDelay, wakeEarly) = wake switch
        {
            WakeTurbulenceCategory.Light => (0, 0),
            WakeTurbulenceCategory.Medium => (1, 0),
            WakeTurbulenceCategory.Heavy => (2, 1),
            WakeTurbulenceCategory.Super => (3, 2),
            _ => (0, 0)
        };

        var delayJitter = rng.Next(-12, 13);
        var earlyJitter = flightType == FlightType.Arrival
            ? rng.Next(-2, 3)
            : rng.Next(-3, 4);

        var maxDelay = baseDelay + wakeDelay + delayJitter;
        var maxEarly = baseEarly + wakeEarly + earlyJitter;

        if (maxDelay < 5) maxDelay = 5;
        if (maxDelay > 45) maxDelay = 45;

        var maxEarlyCap = flightType == FlightType.Arrival ? 5 : 10;
        if (maxEarly < 0) maxEarly = 0;
        if (maxEarly > maxEarlyCap) maxEarly = maxEarlyCap;

        return (maxDelay, maxEarly);
    }

    private static int GeneratePriority(Random rng)
    {
        var roll = rng.Next(100);

        if (roll < 35) return 1;
        if (roll < 62) return 2;
        if (roll < 82) return 3;
        if (roll < 95) return 4;
        return 5;
    }

    private static DateTime RoundToNearestMinute(DateTime value)
    {
        var truncated = new DateTime(
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            0,
            value.Kind);

        return value.Second >= 30 ? truncated.AddMinutes(1) : truncated;
    }
}
