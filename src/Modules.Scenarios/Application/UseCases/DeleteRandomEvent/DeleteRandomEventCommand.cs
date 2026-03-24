using MediatR;

namespace Modules.Scenarios.Application.UseCases.DeleteRandomEvent;

public sealed record DeleteRandomEventCommand(Guid Id) : IRequest<bool>;