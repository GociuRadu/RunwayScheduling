namespace Modules.Solver.Domain;

public enum CancellationReason
{
    None,
    NoCompatibleRunway,
    OutsideScenarioWindow,
    ExceedsMaxDelay
}
 