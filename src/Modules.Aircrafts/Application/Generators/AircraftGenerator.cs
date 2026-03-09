using Modules.Aircrafts.Domain;
using AircraftEntity = Modules.Aircrafts.Domain.Aircraft;

namespace Modules.Aircrafts.Application.Generators;

public static class AircraftGenerator
{
    private static readonly Random _rand = Random.Shared;

    private static string Pick(params string[] options)
    {
        int index = _rand.Next(options.Length);
        return options[index];
    }

    public static AircraftEntity Generate(string tailNumber, WakeTurbulenceCategory wakeCategory)
    {
        string model;
        int passengers;

        switch (wakeCategory)
        {
            case WakeTurbulenceCategory.Light:
                model = Pick(
                    "Cessna 172",
                    "Piper PA-28 Cherokee",
                    "Diamond DA40",
                    "Cirrus SR22"
                );
                passengers = _rand.Next(2, 6);
                break;

            case WakeTurbulenceCategory.Medium:
                model = Pick(
                    "Airbus A320",
                    "Boeing 737-800",
                    "Embraer E195",
                    "Airbus A220-300"
                );
                passengers = _rand.Next(70, 180);
                break;

            case WakeTurbulenceCategory.Heavy:
                model = Pick(
                    "Boeing 777-300ER",
                    "Airbus A350-900",
                    "Boeing 787-9",
                    "Airbus A330-300"
                );
                passengers = _rand.Next(200, 370);
                break;

            case WakeTurbulenceCategory.Super:
                model = Pick(
                    "Boeing 747-8F",
                    "Airbus BelugaXL",
                    "Antonov An-124",
                    "Boeing 777F"
                );
                passengers = _rand.Next(2, 20);
                break;

            default:
                model = "Unknown";
                passengers = 0;
                break;
        }

        return new AircraftEntity
        {
            TailNumber = tailNumber,
            Model = model,
            MaxPassengers = passengers,
            WakeCategory = wakeCategory
        };
    }
}
