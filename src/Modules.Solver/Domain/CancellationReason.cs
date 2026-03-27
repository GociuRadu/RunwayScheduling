namespace Modules.Solver.Domain;

public enum CancellationReason
{
    None = 0,
    OutsideScenarioWindow = 1,
    ExceedsMaxDelay = 2,
    NoCompatibleRunway = 3
}
