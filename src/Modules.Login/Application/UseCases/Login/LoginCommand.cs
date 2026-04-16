using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Modules.Login.Application.UseCases.Login;

public sealed record LoginCommand(
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(256)]
    string Email,
    [property: Required]
    [property: StringLength(128, MinimumLength = 8)]
    string Password
) : IRequest<LoginDto>;
