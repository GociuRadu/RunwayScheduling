using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Airports.Domain;
using NSubstitute;

namespace RunwayScheduling.Tests.Unit.Airports;

public sealed class CreateAirportHandlerTests
{
    private readonly IAirportStore _store = Substitute.For<IAirportStore>();
    private readonly CreateAirportHandler _sut;

    public CreateAirportHandlerTests()
    {
        _sut = new CreateAirportHandler(_store);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsStoreAdd()
    {
        // Arrange
        var command = new CreateAirportCommand("LROP", 30, 44.57, 26.09);
        var airport = new Airport { Name = command.Name, StandCapacity = command.StandCapacity, Latitude = command.Latitude, Longitude = command.Longitude };
        _store.AddAsync(Arg.Any<Airport>(), Arg.Any<CancellationToken>()).Returns(airport);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _store.Received(1).AddAsync(Arg.Any<Airport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCreatedAirport()
    {
        // Arrange
        var command = new CreateAirportCommand("LROP", 30, 44.57, 26.09);
        var expected = new Airport { Name = "LROP", StandCapacity = 30, Latitude = 44.57, Longitude = 26.09 };
        _store.AddAsync(Arg.Any<Airport>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("LROP", result.Name);
        Assert.Equal(30, result.StandCapacity);
        Assert.Equal(44.57, result.Latitude);
        Assert.Equal(26.09, result.Longitude);
    }
}
