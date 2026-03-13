using MediatR;

namespace Modules.Login.Application.UseCases.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginDto>;