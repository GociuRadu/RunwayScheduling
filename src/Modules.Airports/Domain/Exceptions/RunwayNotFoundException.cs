namespace Modules.Airports.Domain.Exceptions;

public sealed class RunwayNotFoundException(Guid id)
    : Exception($"Runway '{id}' not found.");
