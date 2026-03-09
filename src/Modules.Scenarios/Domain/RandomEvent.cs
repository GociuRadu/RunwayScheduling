namespace Modules.Scenarios.Domain;

public sealed class RandomEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScenarioConfigId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ImpactPercent { get; set; } = 100;
}