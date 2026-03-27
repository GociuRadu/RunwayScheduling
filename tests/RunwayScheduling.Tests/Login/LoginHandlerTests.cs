using Modules.Login.Application;
using Modules.Login.Application.UseCases.Login;
using Modules.Login.Domain;
using NSubstitute;

namespace RunwayScheduling.Tests.Login;

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
    public async Task Handle_ReturnsToken_WhenCredentialsValid()
    {
        var user = new User { Email = "test@test.com", PasswordHash = "hash" };
        _userStore.GetByEmail("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _userStore.Verify("password", "hash").Returns(true);
        _tokenService.GenerateToken(user).Returns("jwt-token");

        var result = await _sut.Handle(new LoginCommand("test@test.com", "password"), CancellationToken.None);

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        _userStore.GetByEmail(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(new LoginCommand("nobody@test.com", "pass"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenPasswordInvalid()
    {
        var user = new User { Email = "test@test.com", PasswordHash = "hash" };
        _userStore.GetByEmail("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _userStore.Verify("wrong", "hash").Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(new LoginCommand("test@test.com", "wrong"), CancellationToken.None));
    }
}
