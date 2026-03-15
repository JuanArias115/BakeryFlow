using BakeryFlow.Domain.Entities;

namespace BakeryFlow.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
