using Modules.Login.Domain;

namespace Modules.Login.Application;

public interface IUserStore
{
    Task<User?> GetByEmail(string email, CancellationToken ct);
    bool Verify(string password, string passwordHash);
}