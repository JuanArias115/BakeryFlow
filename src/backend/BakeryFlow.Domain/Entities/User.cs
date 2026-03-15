using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = SystemRoles.Admin;

    public bool IsActive { get; set; } = true;
}
