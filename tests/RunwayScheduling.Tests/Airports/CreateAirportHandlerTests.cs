using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Airports.Domain;
using NSubstitute;

namespace RunwayScheduling.Tests.Airports;

public sealed class CreateAirportHandlerTests
{
    private readonly IAirportStore _store = Substitute.For<IAirportStore>();
    private readonly CreateAirportHandler _sut;

    public CreateAirportHandlerTests()
    {
        _sut = new CreateAirportHandler(_store);
    }

    [Fact]
    public async Task Handle_ReturnsAirportWithCorrectProperties()
    {
        var command = new CreateAirportCommand("OTP", 30, 44.5, 26.1);
        _store.Add(Arg.Any<Airport>()).Returns(x => x.Arg<Airport>());

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.Equal("OTP", result.Name);
        Assert.Equal(30, result.StandCapacity);
        Assert.Equal(44.5, result.Latitude);
        Assert.Equal(26.1, result.Longitude);
    }

    [Fact]
    public async Task Handle_CallsStoreAdd()
    {
        var command = new CreateAirportCommand("OTP", 20, 0, 0);
        _store.Add(Arg.Any<Airport>()).Returns(x => x.Arg<Airport>());

        await _sut.Handle(command, CancellationToken.None);

        _store.Received(1).Add(Arg.Is<Airport>(a => a.Name == "OTP"));
    }
}
