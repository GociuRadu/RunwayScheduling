namespace Modules.Airports.Domain.Exceptions;

public sealed class AirportNotFoundException(Guid id)
    : Exception($"Airport '{id}' not found.");
