namespace Modules.Aircrafts.Application.UseCases.GetAircrafts;

using Modules.Aircrafts.Domain;
public sealed record GetAircraftsDto(
    Guid Id,
    string TailNumber,
     string Model,
     int MaxPassengers,
 WakeTurbulenceCategory WakeCategory
);