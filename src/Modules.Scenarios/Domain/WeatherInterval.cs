namespace Modules.Scenarios.Domain;

public sealed class WeatherInterval
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScenarioConfigId { get; set; } = Guid.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public WeatherCondition WeatherType { get; set; } = WeatherCondition.Clear;
}
