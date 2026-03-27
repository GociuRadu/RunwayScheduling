using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.GetAirports;
using Modules.Airports.Domain;
using NSubstitute;

namespace RunwayScheduling.Tests.Airports;

public sealed class GetAirportsHandlerTests
{
    private readonly IAirportStore _store = Substitute.For<IAirportStore>();
    private readonly GetAirportsHandler _sut;

    public GetAirportsHandlerTests()
    {
        _sut = new GetAirportsHandler(_store);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoAirports()
    {
        _store.GetAll().Returns([]);

        var result = await _sut.Handle(new GetAirportsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var airport = new Airport { Name = "OTP", StandCapacity = 25, Latitude = 44.5, Longitude = 26.1 };
        _store.GetAll().Returns([airport]);

        var result = await _sut.Handle(new GetAirportsQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("OTP", result[0].Name);
        Assert.Equal(25, result[0].StandCapacity);
        Assert.Equal(airport.Id, result[0].Id);
    }

    [Fact]
    public async Task Handle_ReturnsAllAirports()
    {
        _store.GetAll().Returns([
            new Airport { Name = "OTP" },
            new Airport { Name = "CLJ" },
            new Airport { Name = "TSR" }
        ]);

        var result = await _sut.Handle(new GetAirportsQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}
