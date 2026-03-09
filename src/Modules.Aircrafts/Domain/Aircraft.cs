namespace Modules.Aircrafts.Domain;

public class Aircraft
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScenarioConfigId { get; set; } = Guid.Empty;
    public string TailNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxPassengers { get; set; } = 0;
    public WakeTurbulenceCategory WakeCategory { get; set; } = WakeTurbulenceCategory.Light;
}

