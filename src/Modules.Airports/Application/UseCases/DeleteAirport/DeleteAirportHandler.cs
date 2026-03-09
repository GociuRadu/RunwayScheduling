using MediatR;
namespace Modules.Airports.Application.UseCases.DeleteAirport;

public sealed class DeleteAirportHandler : IRequestHandler<DeleteAirportCommand, bool>
{
    public readonly IAirportStore _store;

    public DeleteAirportHandler(IAirportStore store) => _store = store;

    public async Task<bool> Handle(DeleteAirportCommand request, CancellationToken ct)
    {
        return await _store.Delete(request.AirportId, ct);
    }
}