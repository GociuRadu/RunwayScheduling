using Modules.Login.Domain;

namespace Modules.Login.Application;

public interface ITokenService
{
    string GenerateToken(User user);
}