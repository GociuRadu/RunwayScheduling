using MediatR;

namespace Modules.Login.Application.UseCases.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginDto>
{
    private readonly IUserStore _userStore;
    private readonly ITokenService _tokenService;

    public LoginHandler(
        IUserStore userStore,
        ITokenService tokenService)
    {
        _userStore = userStore;
        _tokenService = tokenService;
    }

    public async Task<LoginDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = await _userStore.GetByEmail(email, ct);

        if (user is null || !_userStore.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = _tokenService.GenerateToken(user);

        return new LoginDto(accessToken);
    }
}
