using MediatR;
using Modules.Airports.Application;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.CreateRunway;


public sealed class CreateRunwayHandler : IRequestHandler<CreateRunwayCommand, CreateRunwayResult>
{
    private readonly IRunwayStore _store;

    public CreateRunwayHandler(IRunwayStore store)
    {
        _store = store;
    }

    public Task<CreateRunwayResult> Handle(CreateRunwayCommand request, CancellationToken ct)
    {
        var runway = new Runway
        {
            AirportId = request.AirportId,
            Name = request.Name,
            IsActive = request.IsActive,
            RunwayType = request.RunwayType
        };

        var saved = _store.Add(runway);

        return Task.FromResult(new CreateRunwayResult(
            saved.Id,
            saved.AirportId,
            saved.Name,
            saved.IsActive,
            saved.RunwayType
        ));
    }
}
