using MediatR;
namespace Modules.Airports.Application.UseCases.DeleteRunway;

public sealed class DeleteRunwayHandler : IRequestHandler<DeleteRunwayCommand, bool>
{
    public readonly IRunwayStore _store;

    public DeleteRunwayHandler(IRunwayStore store) => _store = store;

    public async Task<bool> Handle(DeleteRunwayCommand request, CancellationToken ct)
    {
        return await _store.Delete(request.RunwayId, ct);
    }
}