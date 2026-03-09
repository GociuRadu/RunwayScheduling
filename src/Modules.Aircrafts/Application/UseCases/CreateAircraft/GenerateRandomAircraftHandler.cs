using MediatR;
using Modules.Aircrafts.Application;
using Modules.Aircrafts.Application.Generators;
using AircraftEntity = Modules.Aircrafts.Domain.Aircraft;

namespace Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;

public sealed class GenerateRandomAircraftHandler
    : IRequestHandler<GenerateRandomAircraftCommand, List<AircraftEntity>>
{
    private readonly IAircraftStore _store;

    public GenerateRandomAircraftHandler(IAircraftStore store) => _store = store;

    public async Task<List<AircraftEntity>> Handle(GenerateRandomAircraftCommand request, CancellationToken ct)
    {
        int count = Math.Clamp(request.Count, 1, 500);
        int difficulty = Math.Clamp(request.Difficulty, 1, 5);

        var scenarioId = request.ScenarioConfigId;
        if (scenarioId == Guid.Empty)
            throw new ArgumentException("ScenarioConfigId is required.", nameof(request.ScenarioConfigId));

        var result = new List<AircraftEntity>(capacity: count);

        for (int i = 1; i <= count; i++)
        {
            ct.ThrowIfCancellationRequested();

            string tail = TailNumberGenerator.Generate(i);
            var wake = WakeCategorySelection.Generate(difficulty);
            var aircraft = AircraftGenerator.Generate(tail, wake);

            aircraft.ScenarioConfigId = scenarioId;

            result.Add(aircraft);
        }

        await _store.AddRange(result, ct);

        return result;
    }
}