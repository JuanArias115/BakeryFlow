using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid id, ChangeUserPasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    string Password);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive);

public sealed record ChangeUserPasswordRequest(string Password);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(x => x.Role)
            .Must(UserRoleNormalizer.IsSupportedRole)
            .WithMessage("El rol debe ser Admin u Operator.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(x => x.Role)
            .Must(UserRoleNormalizer.IsSupportedRole)
            .WithMessage("El rol debe ser Admin u Operator.");
    }
}

public sealed class ChangeUserPasswordRequestValidator : AbstractValidator<ChangeUserPasswordRequest>
{
    public ChangeUserPasswordRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class UserService(
    IBakeryFlowDbContext dbContext,
    IPasswordHasher passwordHasher) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Users
            .AsNoTracking()
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) ||
                x.Role.ToLower().Contains(term))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => new UserDto(x.Id, x.FirstName, x.LastName, x.Email, x.Role == SystemRoles.Operator ? SystemRoles.Operator : SystemRoles.Admin, x.IsActive, x.CreatedAt, x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserDto(x.Id, x.FirstName, x.LastName, x.Email, x.Role == SystemRoles.Operator ? SystemRoles.Operator : SystemRoles.Admin, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("Usuario no encontrado.");
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsAvailableAsync(request.Email, null, cancellationToken);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Role = UserRoleNormalizer.Normalize(request.Role),
            IsActive = request.IsActive,
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        await EnsureEmailIsAvailableAsync(request.Email, id, cancellationToken);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim().ToLower();
        user.Role = UserRoleNormalizer.Normalize(request.Role);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid id, ChangeUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.PasswordHash = passwordHasher.Hash(request.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEmailIsAvailableAsync(string email, Guid? userId, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLower();
        var exists = await dbContext.Users.AnyAsync(
            x => x.Email == normalizedEmail && (!userId.HasValue || x.Id != userId.Value),
            cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException("Ya existe un usuario con ese correo.");
        }
    }

}

internal static class UserRoleNormalizer
{
    public static bool IsSupportedRole(string role) =>
        string.Equals(role?.Trim(), SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role?.Trim(), "Administrator", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role?.Trim(), SystemRoles.Operator, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string role) =>
        string.Equals(role?.Trim(), SystemRoles.Operator, StringComparison.OrdinalIgnoreCase)
            ? SystemRoles.Operator
            : SystemRoles.Admin;
}
