using MediatR;
using Modules.Airports.Domain;
using Modules.Airports.Application;
namespace Modules.Airports.Application.UseCases.CreateAirport;

public sealed class CreateAirportHandler : IRequestHandler<CreateAirportCommand, Airport>
{
    private readonly IAirportStore _store;

    public CreateAirportHandler(IAirportStore store)
    {
        _store = store;
    }

    public Task<Airport> Handle(CreateAirportCommand request, CancellationToken ct)
    {
        var airport = new Airport
        {
            Name = request.Name,
            StandCapacity = request.StandCapacity,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        var saved = _store.Add(airport);
        return Task.FromResult(saved);
    }
}