using MediatR;
namespace Modules.Airports.Application.UseCases.UpdateRunway;

public sealed class UpdateRunwayHandler : IRequestHandler<UpdateRunwayCommand, bool>
{
    private readonly IRunwayStore _store;

    public UpdateRunwayHandler(IRunwayStore store) => _store = store;

    public Task<bool> Handle(UpdateRunwayCommand req, CancellationToken ct)
    => _store.Update(req.RunwayId, req.Name, req.IsActive, req.RunwayType, ct);
}