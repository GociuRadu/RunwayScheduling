using Modules.Login.Application;
using Modules.Login.Application.UseCases.Login;
using Modules.Login.Domain;
using NSubstitute;

namespace RunwayScheduling.Tests.Unit.Login;

public sealed class LoginHandlerTests
{
    private readonly IUserStore _userStore = Substitute.For<IUserStore>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _sut = new LoginHandler(_userStore, _tokenService);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginDto()
    {
        var user = new User { Email = "admin@test.com", PasswordHash = "hash" };
        _userStore.GetByEmail("admin@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _userStore.Verify("password123", "hash").Returns(true);
        _tokenService.GenerateToken(user).Returns("jwt-token");
        var command = new LoginCommand("admin@test.com", "password123");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User { Email = "admin@test.com", PasswordHash = "hash" };
        _userStore.GetByEmail("admin@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _userStore.Verify("wrongpassword", "hash").Returns(false);
        var command = new LoginCommand("admin@test.com", "wrongpassword");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsUnauthorizedAccessException()
    {
        _userStore.GetByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var command = new LoginCommand("nobody@test.com", "password");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}
