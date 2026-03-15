using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserDto(Guid Id, string FirstName, string LastName, string Email, string Role);

public sealed record AuthResponse(string Token, CurrentUserDto User);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class AuthService(
    IBakeryFlowDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == request.Email.Trim().ToLower(), cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleException("Credenciales inválidas.");
        }

        var token = jwtTokenService.GenerateToken(user);
        var currentUser = new CurrentUserDto(user.Id, user.FirstName, user.LastName, user.Email, NormalizeRole(user.Role));

        return new AuthResponse(token, currentUser);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId && x.IsActive)
            .Select(x => new CurrentUserDto(x.Id, x.FirstName, x.LastName, x.Email, x.Role == SystemRoles.Operator ? SystemRoles.Operator : SystemRoles.Admin))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("Usuario no encontrado.");
    }

    private static string NormalizeRole(string role) =>
        string.Equals(role, SystemRoles.Operator, StringComparison.OrdinalIgnoreCase)
            ? SystemRoles.Operator
            : SystemRoles.Admin;
}
