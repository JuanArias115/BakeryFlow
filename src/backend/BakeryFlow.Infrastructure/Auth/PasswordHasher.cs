using BakeryFlow.Application.Common.Interfaces;

namespace BakeryFlow.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string value) => BCrypt.Net.BCrypt.HashPassword(value);

    public bool Verify(string value, string hash) => BCrypt.Net.BCrypt.Verify(value, hash);
}
