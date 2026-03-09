namespace Modules.Airports.Domain;

public class Airport
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int StandCapacity { get; set; } = 20;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
