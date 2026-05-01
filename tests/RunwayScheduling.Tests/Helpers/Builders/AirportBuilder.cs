using Modules.Airports.Domain;

namespace RunwayScheduling.Tests.Helpers.Builders;

public sealed class AirportBuilder
{
    private string _name = "Test Airport";
    private int _standCapacity = 20;

    public AirportBuilder WithName(string name) { _name = name; return this; }
    public AirportBuilder WithStandCapacity(int capacity) { _standCapacity = capacity; return this; }

    public Airport Build() => new() { Name = _name, StandCapacity = _standCapacity };
}
