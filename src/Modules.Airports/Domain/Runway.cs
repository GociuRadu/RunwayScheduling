namespace Modules.Airports.Domain;

public class Runway
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AirportId { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public RunwayType RunwayType { get; set; } = RunwayType.Both;
}