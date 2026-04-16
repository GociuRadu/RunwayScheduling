using Microsoft.EntityFrameworkCore;
using Modules.Login.Application;
using Modules.Login.Domain;

namespace Api.DataBase;

public sealed class EfUserStore : IUserStore
{
    private readonly AppDbContext _db;

    public EfUserStore(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmail(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, ct);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
